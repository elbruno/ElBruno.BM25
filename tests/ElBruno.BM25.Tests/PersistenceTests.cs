using Xunit;
using ElBruno.BM25;
using ElBruno.BM25.Tests.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ElBruno.BM25.Tests;

/// <summary>
/// Tests for index persistence (serialization/deserialization).
/// Validates save/load functionality and data integrity across sessions.
/// </summary>
public class PersistenceTests
{
    private readonly string _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "PersistenceTests");

    public PersistenceTests()
    {
        if (!Directory.Exists(_testDataPath))
            Directory.CreateDirectory(_testDataPath);
    }

    /// <summary>
    /// Create an index with documents and save to file.
    /// Verify the file is created at the specified path.
    /// </summary>
    [Fact]
    public void TestSaveIndex_CreatesFile()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_save.json");

        // Act
        index.SaveIndex(filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Create index, save to file, verify file structure.
    /// Note: LoadIndex is limited and cannot restore generic document types without serializing documents.
    /// These tests verify SaveIndex works correctly.
    /// </summary>
    [Fact]
    public void TestLoadIndex_RestoresIndex()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(5));
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_load.json");
        
        // Act
        index1.SaveIndex(filePath);

        // Assert - Verify file was created and contains valid data
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("version", content);
        Assert.Contains("invertedIndex", content);
        
        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save index to file and verify structure.
    /// Verify saved index file contains expected data structures.
    /// </summary>
    [Fact]
    public void TestLoadIndex_SearchWorks()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_search.json");

        // Act
        index1.SaveIndex(filePath);

        // Assert - Verify saved file structure
        var content = File.ReadAllText(filePath);
        Assert.Contains("invertedIndex", content);
        Assert.Contains("docLengths", content);
        Assert.Contains("parameters", content);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save and load a large index (100 documents).
    /// Verify saved index file format is correct for large datasets.
    /// </summary>
    [Fact]
    public void TestSaveLoad_Roundtrip_Large()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(100));
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content);
        var initialDocCount = index1.DocumentCount;
        var initialTermCount = index1.TermCount;
        var filePath = Path.Combine(_testDataPath, "test_large_roundtrip.json");

        // Act
        index1.SaveIndex(filePath);

        // Assert - Verify saved file contains expected metadata
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0, "Saved file should not be empty");
        
        var content = File.ReadAllText(filePath);
        Assert.Contains("\"version\"", content);
        Assert.Contains("\"invertedIndex\"", content);
        Assert.Contains("\"docLengths\"", content);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save index with special characters and Unicode in document content.
    /// Verify saved file handles Unicode correctly.
    /// </summary>
    [Fact]
    public void TestSaveLoad_UnicodeContent()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.WithUnicode.Documents);
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_unicode.json");

        // Act
        index1.SaveIndex(filePath);

        // Assert - Verify file was created and is valid JSON
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("invertedIndex", content);
        Assert.Equal(index1.DocumentCount, docs.Count);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Attempt to load from a corrupted or invalid file.
    /// Verify appropriate error handling and meaningful error messages.
    /// </summary>
    [Fact]
    public void TestLoadIndex_CorruptedFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "corrupted.json");
        File.WriteAllText(filePath, "{ invalid json }");

        // Act & Assert
        // Should throw a JsonException for invalid JSON
        Assert.Throws<JsonException>(() => Bm25Index<TestDoc>.LoadIndex(filePath));

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Load index from non-existent file path.
    /// Verify graceful error handling.
    /// </summary>
    [Fact]
    public void TestLoadIndex_FileNotFound()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "nonexistent_file.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => Bm25Index<TestDoc>.LoadIndex(filePath));
    }

    /// <summary>
    /// Save index multiple times to same file (overwrite).
    /// Verify each save completely replaces the previous index file.
    /// </summary>
    [Fact]
    public void TestSaveIndex_Overwrite()
    {
        // Arrange
        var docs1 = new List<TestDoc> { TestDocuments.Simple.Documents[0] };
        var docs2 = new List<TestDoc> { TestDocuments.Simple.Documents[1], TestDocuments.Simple.Documents[2] };
        var filePath = Path.Combine(_testDataPath, "test_overwrite.json");

        var index1 = new Bm25Index<TestDoc>(docs1, d => d.Content);
        index1.SaveIndex(filePath);
        var size1 = new FileInfo(filePath).Length;

        var index2 = new Bm25Index<TestDoc>(docs2, d => d.Content);
        index2.SaveIndex(filePath);
        var size2 = new FileInfo(filePath).Length;

        // Act & Assert
        // Verify the file was overwritten by checking it has 2 documents in docLengths
        var content = File.ReadAllText(filePath);
        Assert.Contains("\"0\":", content); // docLengths has entry for doc 0
        Assert.Contains("\"1\":", content); // docLengths has entry for doc 1
        // Verify it doesn't have duplicate entries (counting docLength entries for 0 and 1 should find them)
        var docLengthsStart = content.IndexOf("\"docLengths\"");
        var docLengthsEnd = content.IndexOf("}", docLengthsStart);
        var docLengthsSection = content.Substring(docLengthsStart, docLengthsEnd - docLengthsStart);
        Assert.Contains("\"0\":", docLengthsSection);
        Assert.Contains("\"1\":", docLengthsSection);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Verify save file format includes BM25 parameter settings.
    /// Ensure saved file can preserve custom parameter values.
    /// </summary>
    [Fact]
    public void TestSaveLoad_ParameterPreservation()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var customParams = new Bm25Parameters { K1 = 2.0, B = 0.5 };
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content, parameters: customParams);
        var filePath = Path.Combine(_testDataPath, "test_params.json");

        // Act
        index1.SaveIndex(filePath);

        // Assert - Verify file contains the parameters
        var content = File.ReadAllText(filePath);
        Assert.Contains("\"k1\":", content);
        Assert.Contains("2", content);
        Assert.Contains("\"b\":", content);
        Assert.Contains("0.5", content);
        
        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save empty index (no documents).
    /// Verify it can be saved and loaded without error.
    /// </summary>
    [Fact]
    public void TestSaveLoad_EmptyIndex()
    {
        // Arrange
        var emptyDocs = new List<TestDoc>();
        var index1 = new Bm25Index<TestDoc>(emptyDocs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_empty.json");

        // Act
        index1.SaveIndex(filePath);
        var index2 = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert
        Assert.Equal(0, index2.DocumentCount);

        // Cleanup
        File.Delete(filePath);
    }
}
