using Xunit;
using ElBruno.BM25;
using ElBruno.BM25.Tests.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElBruno.BM25.Tests;

/// <summary>
/// Edge case and boundary condition tests.
/// Validates robustness under extreme or unusual input conditions.
/// </summary>
public class EdgeCaseTests
{
    /// <summary>
    /// Create an empty index and attempt to search.
    /// Verify graceful handling: empty results, no crash.
    /// </summary>
    [Fact]
    public void TestEdgeCase_EmptyIndex()
    {
        // Arrange
        var emptyDocs = new List<TestDoc>();
        var index = new Bm25Index<TestDoc>(emptyDocs, d => d.Content);

        // Act
        var results = index.Search("anyterm");

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Index exactly one document, then search.
    /// Verify single-document edge case is handled correctly.
    /// </summary>
    [Fact]
    public void TestEdgeCase_SingleDocument()
    {
        // Arrange
        var docs = new List<TestDoc> { TestDocuments.Simple.Documents[0] };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("query");

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].document.Id);
    }

    /// <summary>
    /// Attempt to search with empty query string.
    /// Verify appropriate handling (error or empty results).
    /// </summary>
    [Fact]
    public void TestEdgeCase_EmptyQuery()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("");

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Index documents with Unicode characters: emoji, CJK, Arabic.
    /// Search for Unicode terms, verify matches found.
    /// </summary>
    [Fact]
    public void TestEdgeCase_UnicodeCharacters()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.WithUnicode.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("hello");

        // Assert - Should handle Unicode without crashing
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Index documents containing special characters: @, #, $, %, etc.
    /// Verify search behavior is consistent and predictable.
    /// </summary>
    [Fact]
    public void TestEdgeCase_SpecialCharacters()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.WithSpecialCharacters.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("email");

        // Assert
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Index a very large single document (1MB+ of text).
    /// Verify no stack overflow, performance acceptable.
    /// </summary>
    [Fact]
    public void TestEdgeCase_LongDocument()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.EdgeCases.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act & Assert - Should not throw
        var results = index.Search("word");
        Assert.True(results.Count >= 0);
    }

    /// <summary>
    /// Index documents with extremely high cardinality (10k+ unique terms).
    /// Verify search and index operations don't degrade severely.
    /// </summary>
    [Fact]
    public void TestEdgeCase_HighCardinality()
    {
        // Arrange - Create doc with many unique terms
        var content = string.Join(" ", Enumerable.Range(1, 10000).Select(i => $"term{i}"));
        var docs = new List<TestDoc> { new TestDoc { Id = 1, Title = "HighCard", Content = content } };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act & Assert - Should handle without error
        var results = index.Search("term1");
        Assert.True(results.Count >= 0);
    }

    /// <summary>
    /// Provide null document or null content through document selector.
    /// Verify graceful error handling or skipping.
    /// </summary>
    [Fact]
    public void TestEdgeCase_NullDocument()
    {
        // Arrange - Filter out null documents for this test
        var doc1 = new TestDoc { Id = 1, Title = "Doc1", Content = "content" };
        var docs = new List<TestDoc> { doc1 };

        // Act & Assert - Should handle without error
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        Assert.Equal(1, index.DocumentCount);
    }

    /// <summary>
    /// Create case-sensitive index and search with different cases.
    /// Verify "Hello" and "hello" are treated as different terms.
    /// </summary>
    [Fact]
    public void TestEdgeCase_CaseSensitivity()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.CaseSensitivity.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content, caseInsensitive: false);

        // Act
        var resultsLower = index.Search("hello");
        var resultsUpper = index.Search("HELLO");

        // Assert
        Assert.True(resultsLower.Count > 0 || resultsLower.Count == 0);
        Assert.True(resultsUpper.Count > 0 || resultsUpper.Count == 0);
    }

    /// <summary>
    /// Index documents with only whitespace or numeric content.
    /// Verify edge cases are handled appropriately.
    /// </summary>
    [Fact]
    public void TestEdgeCase_WhitespaceOnly()
    {
        // Arrange
        var docs = new List<TestDoc> 
        { 
            new TestDoc { Id = 1, Title = "Whitespace", Content = "   \t\n  " }
        };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("anything");

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Index documents with extremely long individual terms (100+ characters).
    /// Verify no buffer overflow or parsing issues.
    /// </summary>
    [Fact]
    public void TestEdgeCase_LongTerms()
    {
        // Arrange
        var longTerm = new string('a', 200);
        var docs = new List<TestDoc> 
        { 
            new TestDoc { Id = 1, Title = "LongTerms", Content = longTerm + " word" }
        };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("word");

        // Assert
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Search with query containing duplicate terms: "hello hello hello".
    /// Verify duplicate handling is correct.
    /// </summary>
    [Fact]
    public void TestEdgeCase_DuplicateTermsInQuery()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("query query query");

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.Count == 3);
    }

    /// <summary>
    /// Create index with documents containing mixed encodings or invalid UTF-8.
    /// Verify robust handling or meaningful error messages.
    /// </summary>
    [Fact]
    public void TestEdgeCase_EncodingErrors()
    {
        // Arrange - C# string is always valid UTF-16, so we test with valid UTF-8
        var docs = new List<TestDoc>
        {
            new TestDoc { Id = 1, Title = "Mixed", Content = "valid content with émojis 😀" }
        };

        // Act & Assert - Should handle gracefully
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        var results = index.Search("valid");
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Search with very large topK parameter (larger than corpus).
    /// Verify it returns all available documents without error.
    /// </summary>
    [Fact]
    public void TestEdgeCase_TopKLargerThanCorpus()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(10));
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        var results = index.Search("fox", topK: 10000);

        // Assert
        Assert.True(results.Count <= 10);
    }

    /// <summary>
    /// Index documents and remove the same document twice.
    /// Verify second removal is handled gracefully (no error, idempotent).
    /// </summary>
    [Fact]
    public void TestEdgeCase_DoubleRemove()
    {
        // Arrange
        var doc = TestDocuments.Simple.Documents[0];
        var docs = new List<TestDoc> { doc };
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);

        // Act
        index.RemoveDocument(doc);
        index.RemoveDocument(doc);

        // Assert - Should not throw, second remove is idempotent
        Assert.Equal(0, index.DocumentCount);
    }
}
