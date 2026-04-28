namespace ElBruno.BM25;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.BM25.Tokenizers;

/// <summary>
/// Main class for building and searching a BM25 full-text search index.
/// Provides high-performance scoring of documents by relevance to queries.
/// </summary>
/// <typeparam name="T">The type of documents to index.</typeparam>
public class Bm25Index<T>
{
    private readonly Func<T, string> _contentSelector;
    private readonly Dictionary<string, Dictionary<T, int>> _invertedIndex = new();
    private readonly Dictionary<T, int> _docLengths = new();
    private readonly Dictionary<string, double> _idfCache = new();
    private readonly List<T> _documents = new();
    private ITokenizer _tokenizer;
    private Bm25Parameters _parameters;
    private bool _caseInsensitive;

    /// <summary>
    /// Initializes a new instance of the Bm25Index class.
    /// </summary>
    /// <param name="documents">The collection of documents to index.</param>
    /// <param name="contentSelector">A function that extracts searchable content from each document.</param>
    /// <param name="tokenizer">The tokenizer to use (defaults to SimpleTokenizer if null).</param>
    /// <param name="parameters">BM25 parameters (defaults to Bm25Parameters.Default if null).</param>
    /// <param name="caseInsensitive">Whether to treat text as case-insensitive (default: true).</param>
    /// <exception cref="ArgumentNullException">Thrown when documents or contentSelector is null.</exception>
    public Bm25Index(
        IEnumerable<T> documents,
        Func<T, string> contentSelector,
        ITokenizer? tokenizer = null,
        Bm25Parameters? parameters = null,
        bool caseInsensitive = true
    )
    {
        _contentSelector = contentSelector ?? throw new ArgumentNullException(nameof(contentSelector));
        _tokenizer = tokenizer ?? new SimpleTokenizer();
        _parameters = parameters ?? Bm25Parameters.Default;
        _caseInsensitive = caseInsensitive;

        Reindex(documents ?? throw new ArgumentNullException(nameof(documents)));
    }

    /// <summary>
    /// Gets the current BM25 parameters.
    /// </summary>
    public Bm25Parameters Parameters
    {
        get => _parameters;
        set => _parameters = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the number of documents in the index.
    /// </summary>
    public int DocumentCount => _documents.Count;

    /// <summary>
    /// Gets the number of unique terms in the index.
    /// </summary>
    public int TermCount => _invertedIndex.Count;

    /// <summary>
    /// Adds a new document to the index.
    /// </summary>
    /// <param name="document">The document to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
    public void AddDocument(T document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));

        _documents.Add(document);
        IndexDocument(document);
        RecalculateAvgDocLength();
    }

    /// <summary>
    /// Removes a document from the index.
    /// </summary>
    /// <param name="document">The document to remove.</param>
    public void RemoveDocument(T document)
    {
        if (!_documents.Remove(document)) return;

        _docLengths.Remove(document);

        foreach (var terms in _invertedIndex.Values)
            terms.Remove(document);

        _invertedIndex.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList()
            .ForEach(term => _invertedIndex.Remove(term));

        _idfCache.Clear();
        RecalculateAvgDocLength();
    }

    /// <summary>
    /// Reindexes the entire collection, replacing all existing data.
    /// </summary>
    /// <param name="documents">The new collection of documents to index.</param>
    /// <exception cref="ArgumentNullException">Thrown when documents is null.</exception>
    public void Reindex(IEnumerable<T> documents)
    {
        _invertedIndex.Clear();
        _docLengths.Clear();
        _idfCache.Clear();
        _documents.Clear();

        var docList = documents?.ToList() ?? throw new ArgumentNullException(nameof(documents));
        _documents.AddRange(docList);

        foreach (var doc in docList)
            IndexDocument(doc);

        RecalculateAvgDocLength();
    }

    /// <summary>
    /// Searches the index for documents matching the query.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="topK">Maximum number of results to return (default: 10).</param>
    /// <param name="threshold">Minimum score threshold for results (default: 0.0).</param>
    /// <param name="ct">Cancellation token for long-running searches.</param>
    /// <returns>A list of (document, score) tuples sorted by score descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public List<(T document, double score)> Search(
        string query,
        int topK = 10,
        double threshold = 0.0,
        CancellationToken ct = default
    )
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var queryTerms = _tokenizer.Tokenize(query);
        if (queryTerms.Count == 0) return new();

        var scores = new Dictionary<T, double>();

        foreach (var term in queryTerms)
        {
            ct.ThrowIfCancellationRequested();

            var normalizedTerm = _caseInsensitive ? _tokenizer.Normalize(term) : term;

            if (!_invertedIndex.TryGetValue(normalizedTerm, out var docsWithTerm)) continue;

            var idf = GetIdf(normalizedTerm);

            foreach (var (doc, tf) in docsWithTerm)
            {
                var docLen = _docLengths[doc];
                var score = CalculateBm25Score(tf, idf, docLen);
                if (!scores.ContainsKey(doc)) scores[doc] = 0;
                scores[doc] += score;
            }
        }

        return scores
            .Where(kv => kv.Value >= threshold)
            .OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>
    /// Searches the index for multiple queries in batch.
    /// </summary>
    /// <param name="queries">A collection of search query strings.</param>
    /// <param name="topK">Maximum number of results per query (default: 10).</param>
    /// <param name="ct">Cancellation token for long-running searches.</param>
    /// <returns>A list of (query, results) tuples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when queries is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<List<(string query, List<(T, double)> results)>> SearchBatch(
        IEnumerable<string> queries,
        int topK = 10,
        CancellationToken ct = default
    )
    {
        if (queries == null) throw new ArgumentNullException(nameof(queries));

        var queryList = queries.ToList();
        var results = new List<(string, List<(T, double)>)>();

        await Task.Run(() =>
        {
            foreach (var query in queryList)
            {
                ct.ThrowIfCancellationRequested();
                var searchResults = Search(query, topK, ct: ct);
                results.Add((query, searchResults));
            }
        }, ct);

        return results;
    }

    /// <summary>
    /// Saves the index to disk in JSON format with optional compression.
    /// </summary>
    /// <param name="filePath">The file path where the index will be saved.</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    public void SaveIndex(string filePath)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));

        var payload = new
        {
            version = "v1",
            tokenizer = _tokenizer.Name,
            parameters = new
            {
                k1 = _parameters.K1,
                b = _parameters.B,
                delta = _parameters.Delta,
                avgDocLength = _parameters.AvgDocLength
            },
            invertedIndex = _invertedIndex.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToDictionary(
                    docEntry => _documents.IndexOf(docEntry.Key).ToString(),
                    docEntry => docEntry.Value
                )
            ),
            docLengths = _docLengths.ToDictionary(
                kv => _documents.IndexOf(kv.Key).ToString(),
                kv => kv.Value
            ),
            idfCache = _idfCache
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads an index from disk.
    /// </summary>
    /// <param name="filePath">The file path to load the index from.</param>
    /// <returns>A new Bm25Index instance loaded from the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file format is invalid.</exception>
    public static Bm25Index<T> LoadIndex(string filePath)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));

        try
        {
            var json = File.ReadAllText(filePath);
            JsonDocument doc;
            
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new JsonException("The index file contains invalid JSON and cannot be parsed.", ex);
            }

            var root = doc.RootElement;

            if (!root.TryGetProperty("version", out var versionElem) || versionElem.GetString() != "v1")
                throw new InvalidOperationException("Invalid index format or unsupported version");

            if (!root.TryGetProperty("tokenizer", out var tokenElem))
                throw new InvalidOperationException("Index file missing tokenizer information");

            var tokenizer = CreateTokenizer(tokenElem.GetString());
            
            if (!root.TryGetProperty("parameters", out var paramsElem))
                throw new InvalidOperationException("Index file missing parameters");

            var parameters = new Bm25Parameters
            {
                K1 = paramsElem.GetProperty("k1").GetDouble(),
                B = paramsElem.GetProperty("b").GetDouble(),
                Delta = paramsElem.GetProperty("delta").GetDouble()
            };
            parameters.SetAvgDocLength(paramsElem.GetProperty("avgDocLength").GetDouble());

            if (!root.TryGetProperty("invertedIndex", out var invertedIndexElem))
                throw new InvalidOperationException("Index file missing inverted index");

            if (!root.TryGetProperty("docLengths", out var docLengthsElem))
                throw new InvalidOperationException("Index file missing document lengths");

            var docCount = docLengthsElem.EnumerateObject().Count();

            // Create placeholder documents for restoration
            var placeholderDocs = Enumerable.Range(0, docCount).Select(_ => default(T)!).ToList();

            // Create a temporary empty index to get access to the private fields
            var tempIndex = new Bm25Index<T>(
                new List<T>(),
                _ => "",
                tokenizer,
                parameters
            );

            // Restore placeholder documents directly to the _documents list
            foreach (var placeholderDoc in placeholderDocs)
            {
                tempIndex._documents.Add(placeholderDoc);
            }

            // Restore inverted index
            foreach (var termProp in invertedIndexElem.EnumerateObject())
            {
                var term = termProp.Name;
                if (string.IsNullOrEmpty(term)) continue;

                var docDict = new Dictionary<T, int>();

                foreach (var docProp in termProp.Value.EnumerateObject())
                {
                    if (int.TryParse(docProp.Name, out var docIdx) && docIdx < tempIndex._documents.Count)
                    {
                        var tf = docProp.Value.GetInt32();
                        if (tf > 0)
                        {
                            var indexedDoc = tempIndex._documents[docIdx];
                            if (indexedDoc != null)
                                docDict[indexedDoc] = tf;
                        }
                    }
                }

                if (docDict.Count > 0)
                    tempIndex._invertedIndex[term] = docDict;
            }

            // Restore document lengths
            foreach (var docLenProp in docLengthsElem.EnumerateObject())
            {
                if (int.TryParse(docLenProp.Name, out var docIdx) && docIdx < tempIndex._documents.Count)
                {
                    var len = docLenProp.Value.GetInt32();
                    if (len >= 0)
                    {
                        var lenDoc = tempIndex._documents[docIdx];
                        if (lenDoc != null)
                            tempIndex._docLengths[lenDoc] = len;
                    }
                }
            }

            // Restore IDF cache
            if (root.TryGetProperty("idfCache", out var idfCacheElem))
            {
                foreach (var idfProp in idfCacheElem.EnumerateObject())
                {
                    var term = idfProp.Name;
                    if (!string.IsNullOrEmpty(term))
                        tempIndex._idfCache[term] = idfProp.Value.GetDouble();
                }
            }

            return tempIndex;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to load index from file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Explains the BM25 score for a document and query, showing component contributions.
    /// Useful for debugging and understanding why a document scored high.
    /// </summary>
    /// <param name="document">The document to explain.</param>
    /// <param name="query">The query string.</param>
    /// <returns>A dictionary showing score breakdown (BM25 score, term scores, length norm, etc.).</returns>
    public Dictionary<string, double> ExplainScore(T document, string query)
    {
        if (query == null) query = "";

        var queryTerms = _tokenizer.Tokenize(query);
        var docLen = _docLengths.TryGetValue(document, out var len) ? len : 0;
        var explanation = new Dictionary<string, double>
        {
            ["document_length"] = docLen,
            ["avg_doc_length"] = _parameters.AvgDocLength,
            ["length_norm"] = _parameters.AvgDocLength > 0 ? docLen / _parameters.AvgDocLength : 0,
            ["total_score"] = 0
        };

        foreach (var term in queryTerms)
        {
            var normalizedTerm = _caseInsensitive ? _tokenizer.Normalize(term) : term;

            if (_invertedIndex.TryGetValue(normalizedTerm, out var docsWithTerm) && docsWithTerm.TryGetValue(document, out var tf))
            {
                var idf = GetIdf(normalizedTerm);
                var score = CalculateBm25Score(tf, idf, docLen);
                explanation[$"term_{term}_tf"] = tf;
                explanation[$"term_{term}_idf"] = idf;
                explanation[$"term_{term}_score"] = score;
                explanation["total_score"] += score;
            }
        }

        return explanation;
    }

    /// <summary>
    /// Provides detailed breakdown of why a document scored a certain way for a BM25 query.
    /// Returns a ScoreExplanation object with per-term analysis.
    /// </summary>
    /// <param name="document">The document to explain.</param>
    /// <param name="query">The query string.</param>
    /// <returns>A ScoreExplanation object containing detailed score breakdown.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public ScoreExplanation ExplainScoreDetailed(T document, string query)
    {
        if (query == null) query = "";

        var queryTerms = _tokenizer.Tokenize(query);
        var docLen = _docLengths.TryGetValue(document, out var len) ? len : 0;
        var explanation = new ScoreExplanation
        {
            DocumentLength = docLen,
            AverageDocumentLength = _parameters.AvgDocLength,
            K1Parameter = _parameters.K1,
            BParameter = _parameters.B,
            LengthNormalization = _parameters.AvgDocLength > 0 ? docLen / _parameters.AvgDocLength : 0,
            TotalScore = 0
        };

        int matchedTerms = 0;

        foreach (var term in queryTerms)
        {
            var normalizedTerm = _caseInsensitive ? _tokenizer.Normalize(term) : term;

            if (_invertedIndex.TryGetValue(normalizedTerm, out var docsWithTerm) && docsWithTerm.TryGetValue(document, out var tf))
            {
                var idf = GetIdf(normalizedTerm);
                var score = CalculateBm25Score(tf, idf, docLen);

                explanation.TermFrequencies[term] = tf;
                explanation.TermIDFs[term] = idf;
                explanation.TermScores[term] = score;
                explanation.TotalScore += score;
                matchedTerms++;
            }
        }

        explanation.MatchedTermCount = matchedTerms;
        return explanation;
    }

    /// <summary>
    /// Gets all unique terms in the index.
    /// </summary>
    /// <returns>A list of all indexed terms.</returns>
    public List<string> GetTerms()
    {
        return new List<string>(_invertedIndex.Keys);
    }

    /// <summary>
    /// Gets all documents that contain a specific term.
    /// </summary>
    /// <param name="term">The term to search for.</param>
    /// <returns>A list of documents containing the term, or empty list if term not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when term is null.</exception>
    public List<T> GetTermDocuments(string term)
    {
        if (term == null) throw new ArgumentNullException(nameof(term));

        var normalizedTerm = _caseInsensitive ? _tokenizer.Normalize(term) : term;

        if (_invertedIndex.TryGetValue(normalizedTerm, out var docsWithTerm))
            return new List<T>(docsWithTerm.Keys);

        return new List<T>();
    }

    /// <summary>
    /// Gets the token length of an indexed document.
    /// </summary>
    /// <param name="document">The document to get the length for.</param>
    /// <returns>The number of tokens in the document, or 0 if not indexed.</returns>
    public int GetDocumentLength(T document)
    {
        return _docLengths.TryGetValue(document, out var len) ? len : 0;
    }

    /// <summary>
    /// Gets statistical information about the index.
    /// </summary>
    /// <returns>A dictionary containing index statistics.</returns>
    public Dictionary<string, object> GetStatistics()
    {
        var totalTokens = _docLengths.Values.Sum();
        var avgDocLength = _documents.Count > 0 ? totalTokens / (double)_documents.Count : 0;

        return new Dictionary<string, object>
        {
            ["document_count"] = _documents.Count,
            ["term_count"] = _invertedIndex.Count,
            ["total_tokens"] = totalTokens,
            ["average_document_length"] = avgDocLength,
            ["k1"] = _parameters.K1,
            ["b"] = _parameters.B,
            ["delta"] = _parameters.Delta,
            ["tokenizer"] = _tokenizer.Name,
            ["case_insensitive"] = _caseInsensitive,
            ["vocabulary_richness"] = _documents.Count > 0 ? _invertedIndex.Count / (double)_documents.Count : 0
        };
    }

    private void IndexDocument(T document)
    {
        var content = _contentSelector(document) ?? "";
        var tokens = _tokenizer.Tokenize(content);
        
        if (tokens == null || tokens.Count == 0)
        {
            _docLengths[document] = 0;
            return;
        }

        var termFreqs = new Dictionary<string, int>();

        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token)) continue;

            var normalizedTerm = _caseInsensitive ? _tokenizer.Normalize(token) : token;
            if (!string.IsNullOrEmpty(normalizedTerm))
            {
                if (!termFreqs.ContainsKey(normalizedTerm))
                    termFreqs[normalizedTerm] = 0;
                termFreqs[normalizedTerm]++;
            }
        }

        _docLengths[document] = tokens.Count;

        foreach (var (term, freq) in termFreqs)
        {
            if (!_invertedIndex.ContainsKey(term))
                _invertedIndex[term] = new();
            _invertedIndex[term][document] = freq;
        }

        _idfCache.Clear();
    }

    private double GetIdf(string term)
    {
        if (_idfCache.TryGetValue(term, out var cached))
            return cached;

        var docFreq = _invertedIndex.TryGetValue(term, out var docs) ? docs.Count : 0;
        var totalDocs = _documents.Count;

        if (totalDocs == 0) return 0;

        var idf = Math.Log(1 + (totalDocs - docFreq + 0.5) / (docFreq + 0.5));
        _idfCache[term] = idf;

        return idf;
    }

    private double CalculateBm25Score(int tf, double idf, int docLen)
    {
        var k1 = _parameters.K1;
        var b = _parameters.B;

        var numerator = (k1 + 1) * tf;
        var denominator = tf + k1 * (1 - b + b * (docLen / Math.Max(_parameters.AvgDocLength, 1)));

        return idf * (numerator / denominator);
    }

    private void RecalculateAvgDocLength()
    {
        var avgLen = _documents.Count > 0
            ? _docLengths.Values.Sum() / (double)_documents.Count
            : 0;

        _parameters.SetAvgDocLength(avgLen);
    }

    private static ITokenizer CreateTokenizer(string? name) => name?.ToLower() switch
    {
        "english" => new EnglishTokenizer(),
        "simple" => new SimpleTokenizer(),
        _ => new SimpleTokenizer()
    };
}
