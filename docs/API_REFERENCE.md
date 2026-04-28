# API Reference

Complete documentation of all public classes and methods in ElBruno.BM25.

---

## Bm25Index<T>

Main class for building and searching a BM25 full-text search index.

### Constructor

```csharp
public Bm25Index(
    IEnumerable<T> documents,
    Func<T, string> contentSelector,
    ITokenizer? tokenizer = null,
    Bm25Parameters? parameters = null,
    bool caseInsensitive = true
)
```

**Parameters:**
- `documents` — Collection of documents to index
- `contentSelector` — Function to extract searchable text from each document
- `tokenizer` — Tokenizer to use (default: `SimpleTokenizer`)
- `parameters` — BM25 parameters (default: `Bm25Parameters.Default`)
- `caseInsensitive` — Whether search is case-insensitive (default: `true`)

**Exceptions:**
- `ArgumentNullException` — Thrown if documents or contentSelector is null

**Example:**
```csharp
var index = new Bm25Index<Article>(
    articles,
    article => article.Content,
    new EnglishTokenizer(),
    Bm25Parameters.Aggressive
);
```

---

### Properties

#### DocumentCount
```csharp
public int DocumentCount { get; }
```
Gets the number of documents in the index.

#### TermCount
```csharp
public int TermCount { get; }
```
Gets the number of unique terms indexed.

#### Parameters
```csharp
public Bm25Parameters Parameters { get; set; }
```
Gets or sets the BM25 parameters. Setting to null throws `ArgumentNullException`.

---

### Search Methods

#### Search

```csharp
public List<(T document, double score)> Search(
    string query,
    int topK = 10,
    double threshold = 0.0,
    CancellationToken ct = default
)
```

Searches the index for documents matching the query.

**Parameters:**
- `query` — Search query string
- `topK` — Maximum number of results to return (default: 10)
- `threshold` — Minimum score threshold for results (default: 0.0)
- `ct` — Cancellation token for long-running operations

**Returns:** List of (document, score) tuples sorted by score descending

**Exceptions:**
- `ArgumentNullException` — If query is null
- `OperationCanceledException` — If operation is cancelled

**Example:**
```csharp
var results = index.Search("machine learning", topK: 5, threshold: 0.5);
foreach (var (doc, score) in results)
{
    Console.WriteLine($"{doc.Title}: {score:F2}");
}
```

#### SearchBatch

```csharp
public async Task<List<(string query, List<(T, double)> results)>> SearchBatch(
    IEnumerable<string> queries,
    int topK = 10,
    CancellationToken ct = default
)
```

Searches the index for multiple queries in batch.

**Parameters:**
- `queries` — Collection of search query strings
- `topK` — Maximum number of results per query (default: 10)
- `ct` — Cancellation token for long-running operations

**Returns:** List of (query, results) tuples

**Exceptions:**
- `ArgumentNullException` — If queries is null
- `OperationCanceledException` — If operation is cancelled

**Example:**
```csharp
var queries = new[] { "machine learning", "neural networks" };
var batchResults = await index.SearchBatch(queries, topK: 5);

foreach (var (query, results) in batchResults)
{
    Console.WriteLine($"Query: {query} - {results.Count} results");
}
```

---

### Document Management

#### AddDocument

```csharp
public void AddDocument(T document)
```

Adds a new document to the index.

**Parameters:**
- `document` — The document to add

**Exceptions:**
- `ArgumentNullException` — If document is null

**Example:**
```csharp
var newArticle = new Article { Title = "New Topic", Content = "..." };
index.AddDocument(newArticle);
```

#### RemoveDocument

```csharp
public void RemoveDocument(T document)
```

Removes a document from the index.

**Parameters:**
- `document` — The document to remove

**Returns:** Nothing (silently ignores if document not found)

**Example:**
```csharp
index.RemoveDocument(oldArticle);
```

#### Reindex

```csharp
public void Reindex(IEnumerable<T> documents)
```

Reindexes the entire collection, replacing all existing data.

**Parameters:**
- `documents` — The new collection of documents to index

**Exceptions:**
- `ArgumentNullException` — If documents is null

**Example:**
```csharp
var updatedArticles = LoadLatestArticles();
index.Reindex(updatedArticles);
```

---

### Persistence

#### SaveIndex

```csharp
public void SaveIndex(string filePath)
```

Saves the index to disk in JSON format.

**Parameters:**
- `filePath` — File path where index will be saved

**Exceptions:**
- `ArgumentNullException` — If filePath is null
- `IOException` — If the file cannot be written

**Example:**
```csharp
index.SaveIndex("my_index.json");
```

#### LoadIndex (static)

```csharp
public static Bm25Index<T> LoadIndex(string filePath)
```

Loads an index from disk.

**Parameters:**
- `filePath` — File path to load the index from

**Returns:** New Bm25Index<T> instance

**Exceptions:**
- `ArgumentNullException` — If filePath is null
- `FileNotFoundException` — If file doesn't exist
- `JsonException` — If JSON is invalid
- `InvalidOperationException` — If file format is invalid

**Example:**
```csharp
var index = Bm25Index<Article>.LoadIndex("my_index.json");
```

---

### Score Explanation

#### ExplainScore

```csharp
public Dictionary<string, double> ExplainScore(T document, string query)
```

Explains the BM25 score for a document and query as a dictionary.

**Parameters:**
- `document` — Document to explain
- `query` — Query string

**Returns:** Dictionary with keys like `"total_score"`, `"document_length"`, `"term_X_tf"`, `"term_X_idf"`, `"term_X_score"`

**Example:**
```csharp
var explanation = index.ExplainScore(article, "machine learning");
Console.WriteLine($"Total Score: {explanation["total_score"]}");
Console.WriteLine($"Document Length: {explanation["document_length"]}");
```

#### ExplainScoreDetailed

```csharp
public ScoreExplanation ExplainScoreDetailed(T document, string query)
```

Provides detailed breakdown of why a document scored a certain way.

**Parameters:**
- `document` — Document to explain
- `query` — Query string

**Returns:** `ScoreExplanation` object with detailed breakdown

**Example:**
```csharp
var detailed = index.ExplainScoreDetailed(article, "machine learning");
Console.WriteLine($"Matched Terms: {detailed.MatchedTermCount}");
foreach (var (term, score) in detailed.TermScores)
{
    Console.WriteLine($"  '{term}': {score:F3}");
}
```

---

### Information Methods

#### GetTerms

```csharp
public List<string> GetTerms()
```

Gets all unique terms in the index.

**Returns:** List of all indexed terms

**Example:**
```csharp
var terms = index.GetTerms();
Console.WriteLine($"Vocabulary: {string.Join(", ", terms)}");
```

#### GetTermDocuments

```csharp
public List<T> GetTermDocuments(string term)
```

Gets all documents containing a specific term.

**Parameters:**
- `term` — Term to search for

**Returns:** List of documents, or empty list if not found

**Exceptions:**
- `ArgumentNullException` — If term is null

**Example:**
```csharp
var docsWithMachineLearning = index.GetTermDocuments("machine");
Console.WriteLine($"Docs with 'machine': {docsWithMachineLearning.Count}");
```

#### GetDocumentLength

```csharp
public int GetDocumentLength(T document)
```

Gets the token length of an indexed document.

**Parameters:**
- `document` — Document to get length for

**Returns:** Number of tokens in the document, or 0 if not indexed

**Example:**
```csharp
var tokenCount = index.GetDocumentLength(article);
Console.WriteLine($"Article has {tokenCount} tokens");
```

#### GetStatistics

```csharp
public Dictionary<string, object> GetStatistics()
```

Gets statistical information about the index.

**Returns:** Dictionary with keys: `"document_count"`, `"term_count"`, `"total_tokens"`, `"average_document_length"`, `"k1"`, `"b"`, `"delta"`, `"tokenizer"`, `"case_insensitive"`, `"vocabulary_richness"`

**Example:**
```csharp
var stats = index.GetStatistics();
Console.WriteLine($"Documents: {stats["document_count"]}");
Console.WriteLine($"Unique Terms: {stats["term_count"]}");
Console.WriteLine($"Avg Doc Length: {stats["average_document_length"]:F1}");
```

---

## Bm25Parameters

Configuration parameters for the BM25 algorithm.

### Properties

#### K1
```csharp
public double K1 { get; set; } = 1.5
```

Controls term frequency saturation point. Higher values increase impact from term frequency.

**Range:** 0.5 to 2.5 (typical)  
**Default:** 1.5

#### B
```csharp
public double B { get; set; } = 0.75
```

Controls document length normalization.  
- `0.0` — No length normalization  
- `0.75` — Balanced (default)  
- `1.0` — Full length normalization

**Range:** 0.0 to 1.0  
**Default:** 0.75

#### Delta
```csharp
public double Delta { get; set; } = 0.5
```

Smoothing parameter to prevent zero scores on sparse terms.

**Range:** 0.0 to 1.0  
**Default:** 0.5

#### AvgDocLength
```csharp
public double AvgDocLength { get; private set; }
```

Average document length in the corpus (auto-calculated during indexing).

---

### Preset Parameters

#### Default
```csharp
public static Bm25Parameters Default => new();
```
Balanced parameters: k1=1.5, b=0.75, delta=0.5

#### Aggressive
```csharp
public static Bm25Parameters Aggressive => new() { K1 = 2.0, B = 1.0 };
```
For large corpora with many long documents: k1=2.0, b=1.0, delta=0.5

#### Conservative
```csharp
public static Bm25Parameters Conservative => new() { K1 = 1.0, B = 0.5 };
```
For small corpora with consistent document length: k1=1.0, b=0.5, delta=0.5

**Example:**
```csharp
var index = new Bm25Index<Article>(
    articles,
    a => a.Content,
    parameters: Bm25Parameters.Aggressive
);
```

---

## ITokenizer

Pluggable interface for tokenization strategy.

### Properties

#### Name
```csharp
public string Name { get; }
```

Gets the name or variant of the tokenizer (e.g., "English", "Simple").

### Methods

#### Tokenize

```csharp
public List<string> Tokenize(string text)
```

Tokenizes text into a list of terms.

**Parameters:**
- `text` — Input text to tokenize

**Returns:** List of normalized terms

**Example:**
```csharp
var tokenizer = new SimpleTokenizer();
var terms = tokenizer.Tokenize("Hello, world!");
// Returns: ["hello", "world"]
```

#### Normalize

```csharp
public string Normalize(string term)
```

Normalizes a single term (e.g., lowercase, stemming).

**Parameters:**
- `term` — Term to normalize

**Returns:** Normalized term

**Example:**
```csharp
var normalized = tokenizer.Normalize("Running");
// SimpleTokenizer returns: "running"
// EnglishTokenizer returns: "run"
```

---

## SimpleTokenizer

Simple whitespace-based tokenizer with lowercase normalization. No stemming.

```csharp
var tokenizer = new SimpleTokenizer();
var terms = tokenizer.Tokenize("Machine Learning is awesome!");
// Returns: ["machine", "learning", "is", "awesome"]
```

---

## EnglishTokenizer

English-aware tokenizer with Porter stemming. Normalizes morphological variants.

```csharp
var tokenizer = new EnglishTokenizer();
var terms = tokenizer.Tokenize("running authentication");
// Returns: ["run", "authent"]  (stemmed)
```

**Stemming Examples:**
- running → run
- authentication → authent
- computers → comput
- processing → process

---

## CustomTokenizer

Custom tokenizer that allows user-defined tokenization logic.

### Constructor

```csharp
public CustomTokenizer(Func<string, List<string>> tokenizeFn)
```

**Parameters:**
- `tokenizeFn` — Function that tokenizes text into a list of terms

**Exceptions:**
- `ArgumentNullException` — If tokenizeFn is null

**Example:**
```csharp
var customTokenizer = new CustomTokenizer(text =>
{
    // Your domain-specific tokenization logic
    return text
        .ToLower()
        .Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
        .ToList();
});

var index = new Bm25Index<Article>(articles, a => a.Content, customTokenizer);
```

---

## SearchResult<T>

Represents a search result from a BM25 query.

### Properties

#### Document
```csharp
public required T Document { get; set; }
```

The matched document.

#### Score
```csharp
public double Score { get; set; }
```

The BM25 score for this result (unbounded, typically 0-20 for short queries). Higher scores indicate stronger relevance.

#### Rank
```csharp
public double Rank { get; set; }
```

The rank (position) of this result in the result set (1-based).

#### MatchedTerms
```csharp
public List<string> MatchedTerms { get; set; } = new();
```

List of query terms that matched in this document.

#### TermScores
```csharp
public Dictionary<string, double> TermScores { get; set; } = new();
```

Per-term score breakdown showing contribution of each query term to the overall score.

---

## ScoreExplanation

Provides detailed breakdown of why a document scored a certain way for a BM25 query.

### Properties

#### TotalScore
```csharp
public double TotalScore { get; set; }
```

Total BM25 score for the document and query.

#### TermIDFs
```csharp
public Dictionary<string, double> TermIDFs { get; set; } = new();
```

IDF (Inverse Document Frequency) values per query term. IDF measures how rare a term is across the corpus.

#### TermFrequencies
```csharp
public Dictionary<string, double> TermFrequencies { get; set; } = new();
```

TF (Term Frequency) values per query term in the document.

#### LengthNormalization
```csharp
public double LengthNormalization { get; set; }
```

Length normalization factor applied to the document.

#### TermScores
```csharp
public Dictionary<string, double> TermScores { get; set; } = new();
```

Individual BM25 scores per query term. Sum equals TotalScore.

#### MatchedTermCount
```csharp
public int MatchedTermCount { get; set; }
```

Number of unique query terms found in the document.

#### DocumentLength
```csharp
public int DocumentLength { get; set; }
```

Document length (token count).

#### AverageDocumentLength
```csharp
public double AverageDocumentLength { get; set; }
```

Average document length in the corpus.

#### K1Parameter
```csharp
public double K1Parameter { get; set; }
```

K1 parameter used in BM25 calculation.

#### BParameter
```csharp
public double BParameter { get; set; }
```

B parameter used in BM25 calculation.

---

## Bm25Tuner<T>

Optimizes BM25 parameters for a given index using validation queries.

### Constructor

```csharp
public Bm25Tuner(Bm25Index<T> index)
```

**Parameters:**
- `index` — The BM25 index to tune parameters for

**Exceptions:**
- `ArgumentNullException` — If index is null

### Methods

#### TuneAsync

```csharp
public async Task<Bm25Parameters> TuneAsync(
    List<(string query, List<T> relevantDocs)> validationQueries,
    TuningMetric metric = TuningMetric.Recall,
    CancellationToken ct = default
)
```

Automatically tunes BM25 parameters using a validation set.

**Parameters:**
- `validationQueries` — List of (query, relevant_documents) tuples for tuning
- `metric` — Metric to optimize (default: Recall)
- `ct` — Cancellation token

**Returns:** Optimized Bm25Parameters

**Exceptions:**
- `ArgumentNullException` — If validationQueries is null
- `ArgumentException` — If validationQueries is empty

**Example:**
```csharp
var tuner = new Bm25Tuner<Article>(index);
var validationQueries = new List<(string, List<Article>)>
{
    ("machine learning", relevantArticles1),
    ("neural networks", relevantArticles2)
};

var optimized = await tuner.TuneAsync(validationQueries, TuningMetric.F1);
index.Parameters = optimized;
```

#### GridSearchAsync

```csharp
public async Task<List<ParameterTuneResult>> GridSearchAsync(
    List<(string query, List<T> relevantDocs)> validationQueries,
    CancellationToken ct = default
)
```

Performs grid search over parameter space (K1: 0.5-2.5, B: 0-1.0).

**Returns:** List of ParameterTuneResult for each parameter combination

---

## TuningMetric

Enum specifying the metric used to evaluate BM25 parameter tuning.

```csharp
public enum TuningMetric
{
    Precision,  // % of retrieved docs that are relevant
    Recall,     // % of relevant docs that are retrieved
    F1,         // Harmonic mean of precision and recall (recommended)
    NDCG        // Normalized Discounted Cumulative Gain (ranking quality)
}
```

---

## ParameterTuneResult

Result of a single parameter tuning attempt.

### Properties

#### Parameters
```csharp
public required Bm25Parameters Parameters { get; set; }
```

The BM25 parameters used in this tuning attempt.

#### Metric
```csharp
public double Metric { get; set; }
```

The metric value achieved with these parameters.

#### Timestamp
```csharp
public DateTime Timestamp { get; set; } = DateTime.UtcNow;
```

When this tuning was performed.

#### Notes
```csharp
public string? Notes { get; set; }
```

Optional notes about this tuning result.

---

## Exception Types

### ArgumentNullException

Thrown when required parameter is null.

```csharp
try
{
    var index = new Bm25Index<Article>(null, a => a.Content);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Parameter null: {ex.ParamName}");
}
```

### ArgumentException

Thrown when parameter is invalid (e.g., empty validation queries).

### OperationCanceledException

Thrown when operation is cancelled via cancellation token.

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    var results = index.Search(query, ct: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Search was cancelled");
}
```

### IOException

Thrown when file operations fail (SaveIndex, LoadIndex).

### InvalidOperationException

Thrown when loaded index file is invalid or incompatible.

---

## Namespace

All public classes are in the `ElBruno.BM25` namespace:

```csharp
using ElBruno.BM25;
using ElBruno.BM25.Tokenizers;
```
