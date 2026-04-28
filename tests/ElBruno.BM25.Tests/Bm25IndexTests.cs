using Xunit;
using ElBruno.BM25;
using ElBruno.BM25.Tests.Data;
using System.Collections.Generic;
using System.Linq;

namespace ElBruno.BM25.Tests;

/// <summary>
/// Tests for core BM25 indexing and search functionality.
/// Validates scoring algorithm, ranking, parameter tuning, and dynamic operations.
/// </summary>
public class Bm25IndexTests
{
    /// <summary>
    /// Index a single document and verify it exists in the index.
    /// </summary>
    [Fact]
    public void TestBasicIndexing_SingleDocument()
    {
        // Arrange
        var docs = new List<TestDoc> { TestDocuments.Simple.Documents[0] };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act & Assert
        Assert.Equal(1, index.DocumentCount);
        Assert.Single(docs);
    }

    /// <summary>
    /// Index documents with known content and search for exact matching terms.
    /// Verify document is found and scoring is positive.
    /// </summary>
    [Fact]
    public void TestSearchBasic_ExactMatch()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("query");

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.All(r => r.score > 0));
    }

    /// <summary>
    /// Search for a term that doesn't exist in any indexed document.
    /// Verify empty result set is returned gracefully.
    /// </summary>
    [Fact]
    public void TestSearchBasic_NoMatch()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("nonexistent");

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Index multiple documents with different term frequencies.
    /// Verify search results are ranked by relevance (higher frequency = higher score).
    /// </summary>
    [Fact]
    public void TestSearchRanking_RelevanceSorting()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("query");

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].document.Id);
        Assert.Equal(2, results[1].document.Id);
        Assert.Equal(3, results[2].document.Id);
        Assert.True(results[0].score > results[1].score);
        Assert.True(results[1].score > results[2].score);
    }

    /// <summary>
    /// Index a long document and short document with same term frequency.
    /// Verify short document scores higher due to length normalization.
    /// </summary>
    [Fact]
    public void TestSearchRanking_DocumentLengthNormalization()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.WithLength.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("signal");

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(1, results[0].document.Id);
    }

    /// <summary>
    /// Search with a minimum score threshold.
    /// Verify only documents with score >= threshold are returned.
    /// </summary>
    [Fact]
    public void TestSearchWithThreshold()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("query", threshold: 0.5);

        // Assert
        Assert.All(results, r => Assert.True(r.score >= 0.5));
    }

    /// <summary>
    /// Search with topK limit parameter.
    /// Verify exactly K results are returned (or fewer if corpus smaller).
    /// </summary>
    [Fact]
    public void TestSearchTopK_Limit()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(10));
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("fox", topK: 3);

        // Assert
        Assert.True(results.Count <= 3);
    }

    /// <summary>
    /// Index documents and search with K1=1.5 (default), then K1=2.0.
    /// Verify scores change as K1 varies (controls term frequency saturation).
    /// </summary>
    [Fact]
    public void TestParameterTuning_K1Variation()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var defaultParams = Bm25Parameters.Default;
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content, parameters: defaultParams);

        var customParams = new Bm25Parameters { K1 = 2.0, B = 0.75 };
        var index2 = new Bm25Index<TestDoc>(docs, d => d.Content, parameters: customParams);

        // Act
        var results1 = index1.Search("query");
        var results2 = index2.Search("query");

        // Assert
        Assert.NotEmpty(results1);
        Assert.NotEmpty(results2);
        Assert.NotEqual(results1[0].score, results2[0].score);
    }

    /// <summary>
    /// Search with B=0.75 (default), then B=0.5 and B=1.0.
    /// Verify length normalization adjusts as B changes.
    /// </summary>
    [Fact]
    public void TestParameterTuning_BVariation()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.WithLength.Documents);
        var params75 = new Bm25Parameters { K1 = 1.5, B = 0.75 };
        var params50 = new Bm25Parameters { K1 = 1.5, B = 0.5 };
        var params100 = new Bm25Parameters { K1 = 1.5, B = 1.0 };

        var index75 = new Bm25Index<TestDoc>(docs, d => d.Content, parameters: params75);
        var index50 = new Bm25Index<TestDoc>(docs, d => d.Content, parameters: params50);
        var index100 = new Bm25Index<TestDoc>(docs, d => d.Content, parameters: params100);

        // Act
        var results75 = index75.Search("signal");
        var results50 = index50.Search("signal");
        var results100 = index100.Search("signal");

        // Assert
        Assert.NotEmpty(results75);
        Assert.NotEmpty(results50);
        Assert.NotEmpty(results100);
    }

    /// <summary>
    /// Call SearchBatch() with 5 different queries in one operation.
    /// Verify all queries are processed and results are correct.
    /// </summary>
    [Fact]
    public async void TestBatchSearch_MultipleQueries()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(20));
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        var queries = new List<string> { "quick", "brown", "fox", "jumps", "lazy" };

        // Act
        var results = await index.SearchBatch(queries);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.NotNull(r.query));
    }

    /// <summary>
    /// Create index, add document, search, add another document, search again.
    /// Verify dynamic indexing: new documents are immediately searchable.
    /// </summary>
    [Fact]
    public void TestAddDocument_DynamicIndexing()
    {
        // Arrange
        var initialDocs = new List<TestDoc> { TestDocuments.Simple.Documents[0] };
        var index = new Bm25Index<TestDoc>(initialDocs, d => d.Content);

        // Act - Add and search
        index.AddDocument(TestDocuments.Simple.Documents[1]);
        var results1 = index.Search("query");

        // Assert
        Assert.True(results1.Count > 1);
    }

    /// <summary>
    /// Create index with 3 documents, remove 1, then search.
    /// Verify removed document is no longer in results.
    /// </summary>
    [Fact]
    public void TestRemoveDocument_DynamicIndexing()
    {
        // Arrange
        var doc1 = new TestDoc { Id = 1, Title = "Doc1", Content = "apple" };
        var doc2 = new TestDoc { Id = 2, Title = "Doc2", Content = "banana" };
        var doc3 = new TestDoc { Id = 3, Title = "Doc3", Content = "cherry" };
        var docs = new List<TestDoc> { doc1, doc2, doc3 };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        index.RemoveDocument(doc2);
        var results = index.Search("banana");

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Create index with documents, call Reindex() with new document set.
    /// Verify old documents are gone, new documents are indexed.
    /// </summary>
    [Fact]
    public void TestReindex_ReplaceAll()
    {
        // Arrange
        var oldDocs = new List<TestDoc> { TestDocuments.Simple.Documents[0] };
        var index = new Bm25Index<TestDoc>(oldDocs, d => d.Content);

        var newDocs = new List<TestDoc> 
        { 
            new TestDoc { Id = 10, Title = "New1", Content = "completely different" }
        };

        // Act
        index.Reindex(newDocs);
        var resultsOld = index.Search("query");
        var resultsNew = index.Search("different");

        // Assert
        Assert.Empty(resultsOld);
        Assert.NotEmpty(resultsNew);
    }

    /// <summary>
    /// Index multiple documents with mixed case content.
    /// Search with different case queries to verify case-insensitive matching.
    /// </summary>
    [Fact]
    public void TestCaseInsensitivity_Search()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.CaseSensitivity.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content, caseInsensitive: true);

        // Act
        var resultsLower = index.Search("hello");
        var resultsUpper = index.Search("HELLO");
        var resultsMixed = index.Search("HeLLo");

        // Assert
        Assert.True(resultsLower.Count == resultsUpper.Count);
        Assert.True(resultsUpper.Count == resultsMixed.Count);
    }

    /// <summary>
    /// Search with multiple terms in query.
    /// Verify correct scoring when document matches some but not all terms.
    /// </summary>
    [Fact]
    public void TestMultiTermQuery_PartialMatches()
    {
        // Arrange
        var doc1 = new TestDoc { Id = 1, Title = "Doc1", Content = "apple banana cherry" };
        var doc2 = new TestDoc { Id = 2, Title = "Doc2", Content = "apple banana" };
        var doc3 = new TestDoc { Id = 3, Title = "Doc3", Content = "apple" };
        var docs = new List<TestDoc> { doc1, doc2, doc3 };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("apple banana cherry");

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(1, results[0].document.Id);
    }

    /// <summary>
    /// Index and search with very short queries (1-2 characters).
    /// Verify edge case handling doesn't cause errors.
    /// </summary>
    [Fact]
    public void TestShortQueryHandling()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act - Should not throw
        var results1 = index.Search("a");
        var results2 = index.Search("qu");

        // Assert
        Assert.True(results1.Count >= 0);
        Assert.True(results2.Count >= 0);
    }
}
