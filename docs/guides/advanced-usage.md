# Advanced Usage Guide

This guide covers advanced features: parameter tuning, custom tokenizers, persistence, and integration patterns.

---

## Table of Contents

1. [Custom Tokenizers](#custom-tokenizers)
2. [Parameter Tuning](#parameter-tuning)
3. [Score Explanation & Debugging](#score-explanation--debugging)
4. [Persistence Strategies](#persistence-strategies)
5. [Performance Optimization](#performance-optimization)
6. [Integration with MemPalace.NET](#integration-with-mempalacenet)
7. [Hybrid Search](#hybrid-search-rag-patterns)

---

## Custom Tokenizers

### Creating a Domain-Specific Tokenizer

Implement the `ITokenizer` interface for your specific domain:

```csharp
using ElBruno.BM25;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Medical terminology tokenizer with domain-specific normalization.
/// </summary>
public class MedicalTokenizer : ITokenizer
{
    public string Name => "Medical";

    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();

        // Convert to lowercase
        var lower = text.ToLowerInvariant();
        
        // Split on non-alphanumeric
        var terms = System.Text.RegularExpressions.Regex
            .Split(lower, @"[^a-z0-9]+")
            .Where(t => t.Length > 2)  // Filter short terms
            .ToList();

        return terms;
    }

    public string Normalize(string term)
    {
        if (string.IsNullOrEmpty(term)) return string.Empty;
        
        // Apply domain-specific normalization
        var normalized = term.ToLowerInvariant();
        
        // Common medical abbreviation normalization
        normalized = normalized switch
        {
            "dx" => "diagnosis",
            "rx" => "prescription",
            "htn" => "hypertension",
            "dm" => "diabetes",
            _ => normalized
        };

        return normalized;
    }
}
```

**Using the custom tokenizer:**

```csharp
var medicalDocs = new List<MedicalRecord>
{
    new() { Id = 1, Content = "Patient presents with HTN. Rx: Lisinopril" },
    new() { Id = 2, Content = "DX: Diabetes Mellitus. Rx: Metformin" },
    new() { Id = 3, Content = "HTN and DM comorbidities" }
};

var index = new Bm25Index<MedicalRecord>(
    medicalDocs,
    doc => doc.Content,
    new MedicalTokenizer()  // Domain-specific tokenizer
);

// Now "HTN" matches "hypertension" due to custom normalization
var results = index.Search("hypertension");
```

### Using Regular Expressions for Tokenization

Create a tokenizer for code or structured data:

```csharp
using System.Text.RegularExpressions;

public class CodeTokenizer : ITokenizer
{
    public string Name => "Code";

    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();

        var terms = new List<string>();
        
        // Extract code identifiers (camelCase, snake_case, etc.)
        var matches = Regex.Matches(text, @"[a-zA-Z_]\w*|[0-9]+");
        
        foreach (Match match in matches)
        {
            var term = match.Value.ToLowerInvariant();
            if (term.Length > 1)  // Skip single characters
                terms.Add(term);
        }

        return terms;
    }

    public string Normalize(string term)
    {
        // Convert camelCase to separate words for better matching
        var withSpaces = Regex.Replace(term, "([a-z])([A-Z])", "$1 $2");
        return withSpaces.ToLowerInvariant();
    }
}
```

---

## Parameter Tuning

### Automatic Parameter Optimization

Use the `Bm25Tuner` to automatically find optimal parameters:

```csharp
using ElBruno.BM25;

public async Task TuneIndexParameters()
{
    var articles = LoadArticles();
    var index = new Bm25Index<Article>(articles, a => a.Content);

    // Prepare validation queries with known relevant documents
    var validationSet = new List<(string query, List<Article> relevant)>
    {
        ("machine learning", GetRelevantArticles("machine learning")),
        ("neural networks", GetRelevantArticles("neural networks")),
        ("deep learning", GetRelevantArticles("deep learning")),
        ("natural language processing", GetRelevantArticles("nlp")),
        ("computer vision", GetRelevantArticles("computer vision")),
        ("reinforcement learning", GetRelevantArticles("reinforcement learning")),
    };

    // Tune parameters
    var tuner = new Bm25Tuner<Article>(index);
    var optimized = await tuner.TuneAsync(
        validationSet,
        metric: TuningMetric.F1  // Optimize for F1 score (balanced)
    );

    // Apply optimized parameters
    index.Parameters = optimized;
    
    Console.WriteLine($"Optimized K1: {optimized.K1}");
    Console.WriteLine($"Optimized B: {optimized.B}");
    
    return index;
}

private List<Article> GetRelevantArticles(string topic)
{
    // In real scenario: load from database or annotated dataset
    return new List<Article>();
}
```

### Grid Search with Custom Metrics

Perform full grid search to see all combinations:

```csharp
public async Task AnalyzeParameterSpace()
{
    var tuner = new Bm25Tuner<Article>(index);
    
    var validationQueries = new List<(string, List<Article>)>
    {
        ("machine learning", relevantArticles),
    };

    // Grid search tests combinations of K1 and B parameters
    var results = await tuner.GridSearchAsync(validationQueries);

    // Find best result per metric
    foreach (var result in results.OrderByDescending(r => r.Metric).Take(10))
    {
        Console.WriteLine(
            $"K1={result.Parameters.K1:F1}, B={result.Parameters.B:F2}, Score={result.Metric:F3}"
        );
    }
}
```

### Manual Parameter Testing

Test specific parameter combinations:

```csharp
public void TestParameterCombinations()
{
    var index = new Bm25Index<Article>(articles, a => a.Content);
    var query = "machine learning";

    var k1Values = new[] { 1.0, 1.5, 2.0, 2.5 };
    var bValues = new[] { 0.25, 0.5, 0.75, 1.0 };

    foreach (var k1 in k1Values)
    {
        foreach (var b in bValues)
        {
            index.Parameters = new Bm25Parameters { K1 = k1, B = b };
            
            var results = index.Search(query, topK: 5);
            var topScore = results.FirstOrDefault().score;
            
            Console.WriteLine($"K1={k1:F1}, B={b:F2} → Top Score={topScore:F2}");
        }
    }
}
```

---

## Score Explanation & Debugging

### Understanding Why a Document Scored High

Use `ExplainScoreDetailed()` to debug relevance:

```csharp
public void DebugScoring()
{
    var query = "machine learning";
    var document = articles[0];

    // Get detailed explanation
    var explanation = index.ExplainScoreDetailed(document, query);

    Console.WriteLine($"Document: {document.Title}");
    Console.WriteLine($"Query: {query}");
    Console.WriteLine();
    
    Console.WriteLine("Scoring Breakdown:");
    Console.WriteLine($"  Document Length: {explanation.DocumentLength} tokens");
    Console.WriteLine($"  Average Doc Length: {explanation.AverageDocumentLength:F1} tokens");
    Console.WriteLine($"  Length Normalization: {explanation.LengthNormalization:F3}");
    Console.WriteLine();
    
    Console.WriteLine("BM25 Parameters:");
    Console.WriteLine($"  K1: {explanation.K1Parameter}");
    Console.WriteLine($"  B: {explanation.BParameter}");
    Console.WriteLine();
    
    Console.WriteLine("Matched Terms:");
    foreach (var term in explanation.TermScores.Keys)
    {
        var tf = explanation.TermFrequencies[term];
        var idf = explanation.TermIDFs[term];
        var score = explanation.TermScores[term];
        
        Console.WriteLine($"  '{term}':");
        Console.WriteLine($"    Term Frequency (TF): {tf}");
        Console.WriteLine($"    Inverse Document Frequency (IDF): {idf:F4}");
        Console.WriteLine($"    Contribution to Score: {score:F4}");
    }
    
    Console.WriteLine();
    Console.WriteLine($"Total Score: {explanation.TotalScore:F4}");
}
```

**Output Example:**
```
Document: Introduction to Machine Learning
Query: machine learning

Scoring Breakdown:
  Document Length: 25 tokens
  Average Doc Length: 20.5 tokens
  Length Normalization: 1.220

BM25 Parameters:
  K1: 1.5
  B: 0.75

Matched Terms:
  'machine':
    Term Frequency (TF): 3
    Inverse Document Frequency (IDF): 2.0794
    Contribution to Score: 2.1234
  'learning':
    Term Frequency (TF): 2
    Inverse Document Frequency (IDF): 1.9459
    Contribution to Score: 1.3421

Total Score: 3.4655
```

### Comparing Scores Across Documents

Compare how different documents score for the same query:

```csharp
public void CompareDocumentScoring()
{
    var query = "neural networks";
    
    Console.WriteLine($"Score Comparison for Query: '{query}'");
    Console.WriteLine();

    var documents = articles.Take(5);
    var scores = new List<(Article doc, double score, ScoreExplanation explanation)>();

    foreach (var doc in documents)
    {
        var explanation = index.ExplainScoreDetailed(doc, query);
        scores.Add((doc, explanation.TotalScore, explanation));
    }

    // Sort by score descending
    scores = scores.OrderByDescending(s => s.score).ToList();

    Console.WriteLine("Rank | Document | Score | Matched Terms | Avg TF");
    Console.WriteLine("-----|----------|-------|---------------|--------");
    
    for (int i = 0; i < scores.Count; i++)
    {
        var (doc, score, exp) = scores[i];
        var avgTf = exp.TermFrequencies.Values.Average();
        
        Console.WriteLine(
            $"{i+1,4} | {doc.Title.PadRight(20)} | {score:F2} | {exp.MatchedTermCount} | {avgTf:F2}"
        );
    }
}
```

---

## Persistence Strategies

### Save and Load Complete Index

```csharp
public void PersistenceExample()
{
    var index = new Bm25Index<Article>(articles, a => a.Content);
    
    // Save index to JSON
    var indexPath = "my_index_v1.json";
    index.SaveIndex(indexPath);
    Console.WriteLine($"Index saved to {indexPath}");
    
    // Later, load the index
    var restoredIndex = Bm25Index<Article>.LoadIndex(indexPath);
    Console.WriteLine($"Index loaded: {restoredIndex.DocumentCount} documents");
    
    // Verify it works
    var results = restoredIndex.Search("machine learning");
    Console.WriteLine($"Search returned {results.Count} results");
}
```

### Versioning Strategy for Indexes

```csharp
public class IndexManager
{
    private readonly string _indexDirectory;

    public IndexManager(string indexDirectory)
    {
        _indexDirectory = indexDirectory;
        Directory.CreateDirectory(indexDirectory);
    }

    public void SaveIndexVersion(Bm25Index<Article> index, string version)
    {
        var fileName = $"index_v{version}.json";
        var path = Path.Combine(_indexDirectory, fileName);
        
        index.SaveIndex(path);
        Console.WriteLine($"Index version {version} saved");
    }

    public Bm25Index<Article> LoadLatestIndex()
    {
        var files = Directory.GetFiles(_indexDirectory, "index_v*.json")
            .OrderByDescending(f => f)  // Latest first
            .FirstOrDefault();

        if (files == null)
            throw new FileNotFoundException("No index found");

        return Bm25Index<Article>.LoadIndex(files);
    }

    public void CreateSnapshot(Bm25Index<Article> index, string name)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"snapshot_{name}_{timestamp}.json";
        var path = Path.Combine(_indexDirectory, fileName);
        
        index.SaveIndex(path);
        Console.WriteLine($"Snapshot '{name}' created: {fileName}");
    }
}

// Usage
var manager = new IndexManager("./indexes");
manager.SaveIndexVersion(index, "1.0.0");
manager.SaveIndexVersion(index, "1.0.1");
var latest = manager.LoadLatestIndex();
manager.CreateSnapshot(latest, "before-tuning");
```

### Incremental Indexing

```csharp
public class IncrementalIndexer
{
    private Bm25Index<Article> _index;
    private readonly string _indexPath;

    public IncrementalIndexer(string indexPath)
    {
        _indexPath = indexPath;
        
        if (File.Exists(indexPath))
            _index = Bm25Index<Article>.LoadIndex(indexPath);
        else
            _index = new Bm25Index<Article>(new List<Article>(), a => a.Content);
    }

    public void AddNewArticles(List<Article> newArticles)
    {
        foreach (var article in newArticles)
        {
            _index.AddDocument(article);
            Console.WriteLine($"Added: {article.Title}");
        }

        // Save updated index
        _index.SaveIndex(_indexPath);
        Console.WriteLine($"Index updated: {_index.DocumentCount} total documents");
    }

    public void RemoveOutdatedArticles(List<Article> articlesToRemove)
    {
        foreach (var article in articlesToRemove)
        {
            _index.RemoveDocument(article);
        }

        _index.SaveIndex(_indexPath);
    }

    public Bm25Index<Article> GetIndex() => _index;
}
```

---

## Performance Optimization

### Batch Search for Multiple Queries

For multiple searches, use `SearchBatch()` for better efficiency:

```csharp
public async Task BatchSearchPerformance()
{
    var queries = new[]
    {
        "machine learning",
        "neural networks",
        "deep learning",
        "natural language processing",
        "computer vision",
        "reinforcement learning",
        "transfer learning",
        "federated learning"
    };

    Console.WriteLine("Sequential vs Batch Performance:");

    // Sequential search
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var sequentialResults = new List<(string query, List<(Article, double)> results)>();
    foreach (var query in queries)
    {
        sequentialResults.Add((query, index.Search(query, topK: 10)));
    }
    sw.Stop();
    Console.WriteLine($"Sequential: {sw.ElapsedMilliseconds}ms for {queries.Length} queries");

    // Batch search
    sw.Restart();
    var batchResults = await index.SearchBatch(queries, topK: 10);
    sw.Stop();
    Console.WriteLine($"Batch: {sw.ElapsedMilliseconds}ms for {queries.Length} queries");
}
```

### Index Statistics for Optimization

```csharp
public void AnalyzeIndexCharacteristics()
{
    var stats = index.GetStatistics();
    
    var docCount = (int)stats["document_count"];
    var termCount = (int)stats["term_count"];
    var avgDocLength = (double)stats["average_document_length"];
    var vocabRichness = (double)stats["vocabulary_richness"];

    Console.WriteLine("Index Characteristics:");
    Console.WriteLine($"  Documents: {docCount:N0}");
    Console.WriteLine($"  Unique Terms: {termCount:N0}");
    Console.WriteLine($"  Avg Doc Length: {avgDocLength:F1} tokens");
    Console.WriteLine($"  Vocabulary Richness: {vocabRichness:F2}");

    // Recommendations
    Console.WriteLine("\nOptimization Recommendations:");

    if (vocabRichness > 20)
        Console.WriteLine("  ⚠️  High vocabulary richness: Consider using EnglishTokenizer for stemming");
    
    if (avgDocLength > 500)
        Console.WriteLine("  ⚠️  Long documents: Parameter k1=2.0, b=1.0 may help");
    
    if (termCount > docCount * 5)
        Console.WriteLine("  ⚠️  Many terms per document: Consider filtering stop words");
}
```

### Cancellation for Long-Running Searches

```csharp
public async Task SearchWithTimeout()
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    try
    {
        var results = index.Search("machine learning", topK: 100, ct: cts.Token);
        Console.WriteLine($"Search completed: {results.Count} results");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Search was cancelled (timeout)");
    }
}
```

---

## Integration with MemPalace.NET

Integrate ElBruno.BM25 with MemPalace.NET for hybrid memory search:

```csharp
using ElBruno.BM25;
using MemPalace.NET;

public class HybridMemorySearch
{
    private readonly Bm25Index<Memory> _bm25Index;
    private readonly VectorStore _vectorStore;

    public HybridMemorySearch(List<Memory> memories)
    {
        // Initialize BM25 index
        _bm25Index = new Bm25Index<Memory>(
            memories,
            m => m.Content,
            new EnglishTokenizer()
        );

        // Initialize vector store
        _vectorStore = new VectorStore();
        foreach (var memory in memories)
        {
            var embedding = EmbeddingModel.Embed(memory.Content);
            _vectorStore.Add(memory.Id, embedding);
        }
    }

    public async Task<List<Memory>> HybridSearch(string query, int topK = 5)
    {
        // Get BM25 results (lexical search)
        var bm25Results = _bm25Index.Search(query, topK: topK * 2);
        
        // Get vector search results (semantic search)
        var queryEmbedding = EmbeddingModel.Embed(query);
        var vectorResults = _vectorStore.SearchNearest(queryEmbedding, topK: topK * 2);

        // Combine and rank results
        var combined = new Dictionary<string, double>();

        // Add BM25 scores (normalized)
        foreach (var (memory, score) in bm25Results)
        {
            var normalized = score / (bm25Results.First().score + 0.001);
            combined[memory.Id] = normalized;
        }

        // Add vector scores (normalized and weighted)
        foreach (var (id, similarity) in vectorResults)
        {
            if (combined.ContainsKey(id))
                combined[id] = (combined[id] + similarity) / 2;  // Average
            else
                combined[id] = similarity * 0.5;  // Lower weight for vector-only matches
        }

        // Sort and return top results
        var results = combined
            .OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Select(kv => GetMemoryById(kv.Key))
            .ToList();

        return results;
    }

    private Memory GetMemoryById(string id) => /* lookup memory */;
}
```

---

## Hybrid Search (RAG Patterns)

### Combining BM25 with Vector Search

```csharp
public class RAGRetriever
{
    private readonly Bm25Index<Document> _bm25Index;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public RAGRetriever(
        List<Document> documents,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore
    )
    {
        _bm25Index = new Bm25Index<Document>(
            documents,
            d => d.Content,
            new EnglishTokenizer()
        );
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task<List<Document>> RetrieveContext(string query, int topK = 5)
    {
        // Parallel execution for performance
        var bm25Task = Task.Run(() =>
        {
            var results = _bm25Index.Search(query, topK: topK * 2);
            return results.Select(r => (r.document, bm25Score: r.score)).ToList();
        });

        var vectorTask = Task.Run(async () =>
        {
            var embedding = await _embeddingService.EmbedAsync(query);
            var results = await _vectorStore.SearchAsync(embedding, topK * 2);
            return results;
        });

        await Task.WhenAll(bm25Task, vectorTask);

        var bm25Results = await bm25Task;
        var vectorResults = await vectorTask;

        // Hybrid ranking
        var scores = new Dictionary<string, double>();

        // Normalize and add BM25 scores
        var maxBm25 = bm25Results.Max(r => r.bm25Score);
        foreach (var (doc, score) in bm25Results)
        {
            scores[doc.Id] = (score / maxBm25) * 0.4;  // 40% weight
        }

        // Normalize and add vector scores
        var maxVector = vectorResults.Max(r => r.similarity);
        foreach (var (docId, similarity) in vectorResults)
        {
            if (scores.ContainsKey(docId))
                scores[docId] += (similarity / maxVector) * 0.6;  // 60% weight
            else
                scores[docId] = (similarity / maxVector) * 0.6;
        }

        // Return top-k combined results
        var topIds = scores.OrderByDescending(kv => kv.Value).Take(topK).Select(kv => kv.Key);
        return topIds.Select(id => GetDocumentById(id)).ToList();
    }

    private Document GetDocumentById(string id) => /* lookup */;
}
```

---

## Summary

Advanced topics covered:
- ✅ Custom tokenizers for domain-specific needs
- ✅ Automatic parameter tuning with multiple metrics
- ✅ Debugging scores and relevance
- ✅ Persistence and versioning strategies
- ✅ Performance optimization techniques
- ✅ Integration with other systems (MemPalace, RAG)
- ✅ Hybrid search patterns

For more details, see:
- 📚 [API Reference](./API_REFERENCE.md)
- 🏗️ [Architecture](./ARCHITECTURE.md)
- 📖 [Getting Started](./GETTING_STARTED.md)
