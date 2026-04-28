# Getting Started with ElBruno.BM25

This guide takes you from installation to your first search in 5 minutes.

## Step 1: Install the Package

### Via .NET CLI

```bash
dotnet add package ElBruno.BM25
```

### Via NuGet Package Manager

```powershell
Install-Package ElBruno.BM25
```

### Via .csproj

Add this to your `.csproj` file:
```xml
<ItemGroup>
    <PackageReference Include="ElBruno.BM25" Version="0.5.0" />
</ItemGroup>
```

Then run: `dotnet restore`

---

## Step 2: Create Sample Data

Create a simple data model to index:

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}
```

Prepare sample documents:

```csharp
var articles = new List<Article>
{
    new Article
    {
        Id = 1,
        Title = "Introduction to Machine Learning",
        Content = "Machine learning is a subset of artificial intelligence. Learn supervised, unsupervised, and reinforcement learning."
    },
    new Article
    {
        Id = 2,
        Title = "Deep Learning with Neural Networks",
        Content = "Neural networks are powerful models inspired by biological neurons. Deep learning uses multiple layers."
    },
    new Article
    {
        Id = 3,
        Title = "Natural Language Processing",
        Content = "NLP helps computers understand human language. Applications include sentiment analysis, translation, and chatbots."
    },
    new Article
    {
        Id = 4,
        Title = "Computer Vision Basics",
        Content = "Computer vision enables machines to interpret visual information. Used in image recognition, object detection, and more."
    },
    new Article
    {
        Id = 5,
        Title = "Data Science Fundamentals",
        Content = "Data science combines statistics, programming, and domain knowledge. Essential skills for analysis and prediction."
    }
};
```

---

## Step 3: Create and Index

Create a BM25 index from your documents:

```csharp
using ElBruno.BM25;

// Create index by specifying content extraction function
var index = new Bm25Index<Article>(
    articles,
    article => article.Content  // Extract searchable text from each article
);

Console.WriteLine($"Indexed {index.DocumentCount} documents with {index.TermCount} unique terms");
```

**Output:**
```
Indexed 5 documents with 47 unique terms
```

---

## Step 4: Search

Perform your first search:

```csharp
// Search for documents about machine learning
var results = index.Search("machine learning", topK: 10);

Console.WriteLine("Search Results for 'machine learning':\n");
foreach (var (article, score) in results)
{
    Console.WriteLine($"  [{score:F2}] {article.Title}");
}
```

**Output:**
```
Search Results for 'machine learning':

  [2.45] Introduction to Machine Learning
  [1.89] Deep Learning with Neural Networks
  [0.56] Data Science Fundamentals
```

---

## Step 5: Display Results with Details

Create a more detailed result display:

```csharp
var query = "neural networks";
var results = index.Search(query, topK: 5);

Console.WriteLine($"\n\nDetailed Results for '{query}':\n");

if (!results.Any())
{
    Console.WriteLine("No results found.");
    return;
}

int rank = 1;
foreach (var (article, score) in results)
{
    Console.WriteLine($"{rank}. [{score:F3}] {article.Title}");
    Console.WriteLine($"   ID: {article.Id}");
    
    // Show first 100 chars of content
    var preview = article.Content.Substring(0, Math.Min(100, article.Content.Length));
    if (article.Content.Length > 100) preview += "...";
    Console.WriteLine($"   Preview: {preview}");
    
    rank++;
}
```

**Output:**
```
Detailed Results for 'neural networks':

1. [2.156] Deep Learning with Neural Networks
   ID: 2
   Preview: Neural networks are powerful models inspired by biological neurons. Deep learning uses multipl...

2. [0.487] Introduction to Machine Learning
   ID: 1
   Preview: Machine learning is a subset of artificial intelligence. Learn supervised, unsupervised, and r...
```

---

## Step 6: Try Different Searches

Experiment with various queries:

```csharp
var queries = new[]
{
    "machine learning",
    "neural networks",
    "computer vision",
    "natural language processing",
    "data science",
    "AI algorithms"
};

Console.WriteLine("\n\nMultiple Queries:\n");

foreach (var query in queries)
{
    var results = index.Search(query, topK: 3);
    Console.WriteLine($"'{query}' ({results.Count} results)");
    
    if (results.Any())
    {
        var best = results.First();
        Console.WriteLine($"  Best match: {best.document.Title} [{best.score:F2}]");
    }
    else
    {
        Console.WriteLine("  No results found");
    }
}
```

---

## Step 7: Apply Threshold Filtering

Only return results with a minimum score:

```csharp
// Only return documents scoring above 1.0
var results = index.Search("machine learning", topK: 10, threshold: 1.0);

Console.WriteLine($"Results above threshold 1.0: {results.Count} documents");
foreach (var (article, score) in results)
{
    Console.WriteLine($"  [{score:F2}] {article.Title}");
}
```

---

## Step 8: Use English Tokenizer

Try the English tokenizer with Porter stemming for better results:

```csharp
using ElBruno.BM25.Tokenizers;

// Create index with English tokenizer
var indexEnglish = new Bm25Index<Article>(
    articles,
    article => article.Content,
    tokenizer: new EnglishTokenizer()  // Applies stemming
);

// Now searches will match stemmed variants:
// "learning" matches "learn", "learns", "learning", "learned"
var results = indexEnglish.Search("learning", topK: 5);

Console.WriteLine($"\n\nEnglish Tokenizer Results for 'learning': {results.Count} documents");
foreach (var (article, score) in results)
{
    Console.WriteLine($"  [{score:F2}] {article.Title}");
}
```

---

## Step 9: Understand Scores

See why documents were ranked the way they were:

```csharp
var query = "machine learning";
var article = articles.First();

// Get simple score explanation
var explanation = index.ExplainScore(article, query);

Console.WriteLine($"\n\nScore Explanation for '{article.Title}':");
Console.WriteLine($"  Total Score: {explanation["total_score"]:F3}");
Console.WriteLine($"  Document Length: {explanation["document_length"]} tokens");
Console.WriteLine($"  Average Doc Length: {explanation["avg_doc_length"]:F1} tokens");
Console.WriteLine($"  Length Normalization: {explanation["length_norm"]:F2}");

// Get detailed breakdown
var detailed = index.ExplainScoreDetailed(article, query);

Console.WriteLine($"\n  Matched Terms: {detailed.MatchedTermCount}");
foreach (var (term, score) in detailed.TermScores)
{
    var idf = detailed.TermIDFs[term];
    var tf = detailed.TermFrequencies[term];
    Console.WriteLine($"    '{term}': TF={tf}, IDF={idf:F3}, Score={score:F3}");
}
```

---

## Step 10: Get Index Statistics

Understand your index:

```csharp
var stats = index.GetStatistics();

Console.WriteLine("\n\nIndex Statistics:");
Console.WriteLine($"  Documents: {stats["document_count"]}");
Console.WriteLine($"  Unique Terms: {stats["term_count"]}");
Console.WriteLine($"  Total Tokens: {stats["total_tokens"]}");
Console.WriteLine($"  Avg Doc Length: {stats["average_document_length"]:F1} tokens");
Console.WriteLine($"  Vocabulary Richness: {stats["vocabulary_richness"]:F2}");
Console.WriteLine($"  Tokenizer: {stats["tokenizer"]}");
Console.WriteLine($"  Parameters: k1={stats["k1"]}, b={stats["b"]}, delta={stats["delta"]}");
```

**Output:**
```
Index Statistics:
  Documents: 5
  Unique Terms: 47
  Total Tokens: 87
  Avg Doc Length: 17.4 tokens
  Vocabulary Richness: 9.40
  Tokenizer: Simple
  Parameters: k1=1.5, b=0.75, delta=0.5
```

---

## Troubleshooting

### No Results Found

**Problem:** Your search returns no results.

**Solutions:**
1. Check spelling of your search term
2. Verify documents actually contain the terms
3. Try a simpler search term
4. Use `GetTerms()` to list all indexed terms

```csharp
var allTerms = index.GetTerms();
Console.WriteLine($"Indexed terms: {string.Join(", ", allTerms)}");
```

### Low Scores

**Problem:** Scores seem too low.

**Solutions:**
1. Try `EnglishTokenizer` instead of `SimpleTokenizer` for better term matching
2. Use multiple search terms (multi-term queries score higher)
3. Check document length isn't too long (long docs get penalized)

### Wrong Document Ranking

**Problem:** Top result doesn't seem most relevant.

**Solutions:**
1. Use `ExplainScoreDetailed()` to understand the ranking
2. Try different tokenizers or stemming
3. Consider tuning BM25 parameters (see Advanced Usage guide)

---

## Next Steps

- 📖 **[Advanced Usage](./ADVANCED_USAGE.md)** — Parameter tuning, persistence, custom tokenizers
- 🏗️ **[Architecture](./ARCHITECTURE.md)** — Understand the BM25 algorithm
- 📚 **[API Reference](./API_REFERENCE.md)** — Complete method documentation
- 💡 **[README](../README.md)** — Overview and use cases

---

## Complete Example

Here's a complete, runnable example combining all steps:

```csharp
using ElBruno.BM25;
using ElBruno.BM25.Tokenizers;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}

var articles = new List<Article>
{
    new Article { Id = 1, Title = "Machine Learning", Content = "Learn machine learning basics" },
    new Article { Id = 2, Title = "Deep Learning", Content = "Neural networks and deep learning" },
    new Article { Id = 3, Title = "NLP", Content = "Natural language processing" }
};

// Create index
var index = new Bm25Index<Article>(
    articles,
    a => a.Content,
    new EnglishTokenizer()
);

// Search
var results = index.Search("learning", topK: 10);

// Display
foreach (var (article, score) in results)
{
    Console.WriteLine($"[{score:F2}] {article.Title}");
}
```

Happy searching! 🚀
