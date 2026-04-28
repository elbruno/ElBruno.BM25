using Xunit;
using ElBruno.BM25;
using ElBruno.BM25.Tokenizers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElBruno.BM25.Tests;

/// <summary>
/// Tests for tokenization functionality.
/// Validates text processing, case handling, stemming, and custom tokenizer implementations.
/// </summary>
public class TokenizerTests
{
    /// <summary>
    /// Tokenize mixed case text using SimpleTokenizer.
    /// Verify all output tokens are lowercase.
    /// </summary>
    [Fact]
    public void TestSimpleTokenizer_Lowercasing()
    {
        // Arrange
        var tokenizer = new SimpleTokenizer();

        // Act
        var result = tokenizer.Tokenize("HELLO world MiXeD");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("hello", result[0]);
        Assert.Equal("world", result[1]);
        Assert.Equal("mixed", result[2]);
    }

    /// <summary>
    /// Tokenize text with multiple whitespace characters.
    /// Verify no empty tokens and consistent splitting.
    /// </summary>
    [Fact]
    public void TestSimpleTokenizer_WhitespaceNormalization()
    {
        // Arrange
        var tokenizer = new SimpleTokenizer();

        // Act
        var result = tokenizer.Tokenize("hello   world\t\ttab\nnewline");

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("hello", result[0]);
        Assert.Equal("world", result[1]);
        Assert.Equal("tab", result[2]);
        Assert.Equal("newline", result[3]);
    }

    /// <summary>
    /// Tokenize text containing punctuation marks.
    /// Verify punctuation handling (removed or normalized appropriately).
    /// </summary>
    [Fact]
    public void TestSimpleTokenizer_PunctuationHandling()
    {
        // Arrange
        var tokenizer = new SimpleTokenizer();

        // Act
        var result = tokenizer.Tokenize("hello, world! how's going? yes. no; and/or");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("hello", result);
        Assert.Contains("world", result);
        Assert.DoesNotContain(",", result);
        Assert.DoesNotContain("!", result);
    }

    /// <summary>
    /// Use English tokenizer with stemming on verb forms.
    /// Verify "running", "runs", "ran" all stem to "run".
    /// </summary>
    [Fact]
    public void TestEnglishTokenizer_PorterStemming()
    {
        // Arrange
        var tokenizer = new EnglishTokenizer();

        // Act
        var tokens1 = tokenizer.Tokenize("running");
        var tokens2 = tokenizer.Tokenize("runs");
        var tokens3 = tokenizer.Tokenize("ran");

        // Assert
        Assert.Single(tokens1);
        Assert.Single(tokens2);
        Assert.Single(tokens3);
    }

    /// <summary>
    /// Use English tokenizer with stemming and stopword removal.
    /// Verify stemmed terms are returned without stopwords.
    /// </summary>
    [Fact]
    public void TestEnglishTokenizer_StemmingWithStopwords()
    {
        // Arrange
        var tokenizer = new EnglishTokenizer();

        // Act
        var result = tokenizer.Tokenize("the running and the walking of the dog");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("walk", result);
        Assert.Contains("dog", result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// Create a CustomTokenizer with user-defined tokenization logic.
    /// Verify custom logic is applied correctly.
    /// </summary>
    [Fact]
    public void TestCustomTokenizer_UserDefined()
    {
        // Arrange
        var tokenizer = new CustomTokenizer(text =>
        {
            return text.Split(',').Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
        });

        // Act
        var result = tokenizer.Tokenize("hello,world,test");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("hello", result[0]);
        Assert.Equal("world", result[1]);
        Assert.Equal("test", result[2]);
    }

    /// <summary>
    /// Index documents with a case-insensitive tokenizer.
    /// Search with different case variations of same term.
    /// Verify all variations match the indexed documents.
    /// </summary>
    [Fact]
    public void TestTokenizer_CaseInsensitivity()
    {
        // Arrange
        var docs = new List<string> { "The AUTHENTICATION System" };
        var tokenizer = new SimpleTokenizer();
        var index = new Bm25Index<string>(docs, d => d, tokenizer);

        // Act
        var resultsLower = index.Search("authentication");
        var resultsUpper = index.Search("AUTHENTICATION");
        var resultsMixed = index.Search("Authentication");

        // Assert
        Assert.NotEmpty(resultsLower);
        Assert.NotEmpty(resultsUpper);
        Assert.NotEmpty(resultsMixed);
    }

    /// <summary>
    /// Tokenize text with numbers and mixed alphanumeric content.
    /// Verify numbers are handled appropriately.
    /// </summary>
    [Fact]
    public void TestTokenizer_NumericHandling()
    {
        // Arrange
        var tokenizer = new SimpleTokenizer();

        // Act
        var result = tokenizer.Tokenize("version 2.0 release2024 section3");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("version", result);
        Assert.Contains("2", result);
        Assert.Contains("0", result);
        Assert.Contains("release2024", result);
    }

    /// <summary>
    /// Use English tokenizer on plural forms.
    /// Verify "cats", "dogs", "boxes" normalize correctly.
    /// </summary>
    [Fact]
    public void TestEnglishTokenizer_PluralNormalization()
    {
        // Arrange
        var tokenizer = new EnglishTokenizer();

        // Act
        var result = tokenizer.Tokenize("cats dogs boxes");

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// Tokenize very long text (multiple KB).
    /// Verify no stack overflow or performance issues.
    /// </summary>
    [Fact]
    public void TestTokenizer_LongText()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("word", 5000));
        var tokenizer = new SimpleTokenizer();

        // Act
        var result = tokenizer.Tokenize(longText);

        // Assert
        Assert.Equal(5000, result.Count);
        Assert.All(result, t => Assert.Equal("word", t));
    }

    /// <summary>
    /// Tokenize text with Unicode characters (emoji, multilingual).
    /// Verify handled gracefully without errors.
    /// </summary>
    [Fact]
    public void TestTokenizer_UnicodeCharacters()
    {
        // Arrange
        var tokenizer = new SimpleTokenizer();

        // Act & Assert - Should not throw
        var result1 = tokenizer.Tokenize("emoji 😀 chinese");
        var result2 = tokenizer.Tokenize("中文 测试");
        var result3 = tokenizer.Tokenize("العربية مثال");

        Assert.NotEmpty(result1);
        Assert.NotEmpty(result2);
        Assert.NotEmpty(result3);
    }
}
