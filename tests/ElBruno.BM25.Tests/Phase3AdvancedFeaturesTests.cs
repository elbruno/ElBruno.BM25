using Xunit;
using ElBruno.BM25;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElBruno.BM25.Tests;

/// <summary>
/// Tests for Phase 3 advanced features: Score explanation, parameter tuning, statistics, and presets.
/// </summary>
public class Phase3AdvancedFeaturesTests
{
    /// <summary>
    /// Test that ScoreExplanation provides detailed breakdown of scoring.
    /// </summary>
    [Fact]
    public void TestScoreExplanationDetailed_ReturnsCompleteBreakdown()
    {
        // Arrange
        var docs = new List<(int id, string text)>
        {
            (1, "machine learning is a subset of artificial intelligence"),
            (2, "artificial intelligence enables automation"),
            (3, "machine learning models improve with data")
        };

        var index = new Bm25Index<(int id, string text)>(
            docs,
            doc => doc.text
        );

        // Act
        var explanation = index.ExplainScoreDetailed(docs[0], "machine learning");

        // Assert
        Assert.NotNull(explanation);
        Assert.True(explanation.TotalScore > 0);
        Assert.Contains("machine", explanation.TermFrequencies.Keys);
        Assert.Contains("learning", explanation.TermFrequencies.Keys);
        Assert.Equal(2, explanation.MatchedTermCount);
        Assert.Equal(1.5, explanation.K1Parameter);
        Assert.Equal(0.75, explanation.BParameter);
    }

    /// <summary>
    /// Test that ExplainScore still works (backward compatibility).
    /// </summary>
    [Fact]
    public void TestExplainScore_BackwardCompatibility()
    {
        // Arrange
        var docs = new List<string> { "apple banana cherry", "banana cherry date", "date elderberry fig" };
        var index = new Bm25Index<string>(docs, doc => doc);

        // Act
        var explanation = index.ExplainScore(docs[0], "banana cherry");

        // Assert
        Assert.NotNull(explanation);
        Assert.Contains("total_score", explanation.Keys);
        Assert.True(explanation["total_score"] > 0);
    }

    /// <summary>
    /// Test GetTerms returns all indexed terms.
    /// </summary>
    [Fact]
    public void TestGetTerms_ReturnsAllUniqueTerms()
    {
        // Arrange
        var docs = new List<string> { "apple banana", "cherry date", "apple cherry" };
        var index = new Bm25Index<string>(docs, doc => doc);

        // Act
        var terms = index.GetTerms();

        // Assert
        Assert.Equal(4, terms.Count);
        Assert.Contains("apple", terms);
        Assert.Contains("banana", terms);
        Assert.Contains("cherry", terms);
        Assert.Contains("date", terms);
    }

    /// <summary>
    /// Test GetTermDocuments returns correct documents for a term.
    /// </summary>
    [Fact]
    public void TestGetTermDocuments_ReturnsCorrectDocuments()
    {
        // Arrange
        var docs = new List<string> { "apple banana", "cherry date", "apple cherry elderberry" };
        var index = new Bm25Index<string>(docs, doc => doc);

        // Act
        var appleDocuments = index.GetTermDocuments("apple");
        var nonexistentDocs = index.GetTermDocuments("nonexistent");

        // Assert
        Assert.Equal(2, appleDocuments.Count);
        Assert.Contains(docs[0], appleDocuments);
        Assert.Contains(docs[2], appleDocuments);
        Assert.Empty(nonexistentDocs);
    }

    /// <summary>
    /// Test GetDocumentLength returns correct token count.
    /// </summary>
    [Fact]
    public void TestGetDocumentLength_ReturnsCorrectLength()
    {
        // Arrange
        var docs = new List<string> { "one two three", "one two three four five" };
        var index = new Bm25Index<string>(docs, doc => doc);

        // Act
        var length1 = index.GetDocumentLength(docs[0]);
        var length2 = index.GetDocumentLength(docs[1]);

        // Assert
        Assert.Equal(3, length1);
        Assert.Equal(5, length2);
    }

    /// <summary>
    /// Test GetStatistics returns index metadata.
    /// </summary>
    [Fact]
    public void TestGetStatistics_ReturnsIndexMetadata()
    {
        // Arrange
        var docs = new List<string> { "apple banana cherry", "date elderberry fig" };
        var index = new Bm25Index<string>(docs, doc => doc);

        // Act
        var stats = index.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(2, (int)stats["document_count"]);
        Assert.Equal(6, (int)stats["term_count"]);
        Assert.Equal(6, (int)stats["total_tokens"]);
        Assert.Equal(1.5, (double)stats["k1"]);
        Assert.Equal(0.75, (double)stats["b"]);
        Assert.True((double)stats["average_document_length"] > 0);
    }

    /// <summary>
    /// Test parameter presets exist and have correct values.
    /// </summary>
    [Fact]
    public void TestParameterPresets_ExistWithCorrectValues()
    {
        // Act
        var defaultParams = Bm25Parameters.Default;
        var aggressiveParams = Bm25Parameters.Aggressive;
        var conservativeParams = Bm25Parameters.Conservative;

        // Assert
        Assert.Equal(1.5, defaultParams.K1);
        Assert.Equal(0.75, defaultParams.B);
        Assert.Equal(0.5, defaultParams.Delta);

        Assert.Equal(2.0, aggressiveParams.K1);
        Assert.Equal(1.0, aggressiveParams.B);

        Assert.Equal(1.0, conservativeParams.K1);
        Assert.Equal(0.5, conservativeParams.B);
    }

    /// <summary>
    /// Test GridSearchAsync explores parameter space.
    /// </summary>
    [Fact]
    public async Task TestGridSearchAsync_ExploresParameterSpace()
    {
        // Arrange
        var docs = new List<string>
        {
            "machine learning",
            "deep learning",
            "natural language processing"
        };
        var index = new Bm25Index<string>(docs, doc => doc);
        var tuner = new Bm25Tuner<string>(index);

        var validationQueries = new List<(string query, List<string> relevant)>
        {
            ("machine learning", new List<string> { docs[0] }),
            ("language processing", new List<string> { docs[2] })
        };

        // Act
        var results = await tuner.GridSearchAsync(validationQueries);

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.Count > 1);
        Assert.All(results, r => Assert.NotNull(r.Parameters));
        Assert.All(results, r => Assert.True(r.Metric >= 0 && r.Metric <= 1));
    }

    /// <summary>
    /// Test TuneAsync returns optimized parameters.
    /// </summary>
    [Fact]
    public async Task TestTuneAsync_ReturnsOptimizedParameters()
    {
        // Arrange
        var docs = new List<string>
        {
            "neural networks are powerful",
            "deep learning transforms AI",
            "reinforcement learning beats humans"
        };
        var index = new Bm25Index<string>(docs, doc => doc);
        var tuner = new Bm25Tuner<string>(index);

        var validationQueries = new List<(string query, List<string> relevant)>
        {
            ("neural networks", new List<string> { docs[0] }),
            ("deep learning", new List<string> { docs[1] }),
            ("reinforcement learning", new List<string> { docs[2] })
        };

        // Act
        var optimized = await tuner.TuneAsync(validationQueries, TuningMetric.Recall);

        // Assert
        Assert.NotNull(optimized);
        Assert.True(optimized.K1 >= 0.5 && optimized.K1 <= 2.5);
        Assert.True(optimized.B >= 0 && optimized.B <= 1.0);
    }

    /// <summary>
    /// Test tuning with different metrics (Precision, Recall, F1, NDCG).
    /// </summary>
    [Theory]
    [InlineData(TuningMetric.Precision)]
    [InlineData(TuningMetric.Recall)]
    [InlineData(TuningMetric.F1)]
    [InlineData(TuningMetric.NDCG)]
    public async Task TestTuneAsync_WithDifferentMetrics(TuningMetric metric)
    {
        // Arrange
        var docs = new List<string>
        {
            "document one about topic",
            "document two also covers topic",
            "document three is different"
        };
        var index = new Bm25Index<string>(docs, doc => doc);
        var tuner = new Bm25Tuner<string>(index);

        var validationQueries = new List<(string query, List<string> relevant)>
        {
            ("topic", new List<string> { docs[0], docs[1] })
        };

        // Act
        var optimized = await tuner.TuneAsync(validationQueries, metric);

        // Assert
        Assert.NotNull(optimized);
        Assert.True(optimized.K1 > 0);
        Assert.True(optimized.B >= 0);
    }

    /// <summary>
    /// Test that ExplainScoreDetailed distinguishes between matching and non-matching terms.
    /// </summary>
    [Fact]
    public void TestExplainScoreDetailed_PartialMatches()
    {
        // Arrange
        var docs = new List<string>
        {
            "apple banana cherry",
            "date elderberry fig",
            "apple date grapes"
        };
        var index = new Bm25Index<string>(docs, doc => doc);

        // Act
        var explanation = index.ExplainScoreDetailed(docs[0], "apple banana orange");

        // Assert
        Assert.NotNull(explanation);
        Assert.Equal(2, explanation.MatchedTermCount);
        Assert.Contains("apple", explanation.TermScores.Keys);
        Assert.Contains("banana", explanation.TermScores.Keys);
        Assert.DoesNotContain("orange", explanation.TermScores.Keys);
    }

    /// <summary>
    /// Test GetStatistics with different tokenizers.
    /// </summary>
    [Fact]
    public void TestGetStatistics_WithEnglishTokenizer()
    {
        // Arrange
        var docs = new List<string>
        {
            "The quick brown fox",
            "jumps over the lazy dog"
        };
        var tokenizer = new Tokenizers.EnglishTokenizer();
        var index = new Bm25Index<string>(docs, doc => doc, tokenizer);

        // Act
        var stats = index.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(2, (int)stats["document_count"]);
        Assert.Equal("English", (string)stats["tokenizer"]);
    }

    /// <summary>
    /// Test that AddDocument is reflected in GetStatistics.
    /// </summary>
    [Fact]
    public void TestGetStatistics_UpdatesAfterAddDocument()
    {
        // Arrange
        var docs = new List<string> { "apple banana" };
        var index = new Bm25Index<string>(docs, doc => doc);

        var statsBefore = index.GetStatistics();

        // Act
        index.AddDocument("cherry date");
        var statsAfter = index.GetStatistics();

        // Assert
        Assert.Equal(1, (int)statsBefore["document_count"]);
        Assert.Equal(2, (int)statsAfter["document_count"]);
        Assert.True((int)statsAfter["term_count"] > (int)statsBefore["term_count"]);
    }

    /// <summary>
    /// Test that RemoveDocument is reflected in GetStatistics.
    /// </summary>
    [Fact]
    public void TestGetStatistics_UpdatesAfterRemoveDocument()
    {
        // Arrange
        var doc1 = "apple banana";
        var doc2 = "cherry date";
        var docs = new List<string> { doc1, doc2 };
        var index = new Bm25Index<string>(docs, doc => doc);

        var statsBefore = index.GetStatistics();

        // Act
        index.RemoveDocument(doc1);
        var statsAfter = index.GetStatistics();

        // Assert
        Assert.Equal(2, (int)statsBefore["document_count"]);
        Assert.Equal(1, (int)statsAfter["document_count"]);
    }
}
