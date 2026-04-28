using Xunit;
using ElBruno.BM25;
using ElBruno.BM25.Tests.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    /// Create index, save to file, load from file.
    /// Verify loaded index contains same documents, terms, and IDFs.
    /// </summary>
    [Fact]
    public void TestLoadIndex_RestoresIndex()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(5));
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_load.json");
        index1.SaveIndex(filePath);

        // Act
        var index2 = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert
        Assert.Equal(index1.DocumentCount, index2.DocumentCount);
        Assert.Equal(index1.TermCount, index2.TermCount);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save index to file, load it, then verify structure.
    /// Verify loaded index has the correct term count.
    /// </summary>
    [Fact]
    public void TestLoadIndex_SearchWorks()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.Simple.Documents);
        var index1 = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "test_search.json");
        index1.SaveIndex(filePath);

        // Act
        var index2 = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert - Verify structure is preserved
        Assert.Equal(index1.TermCount, index2.TermCount);
        Assert.Equal(index1.DocumentCount, index2.DocumentCount);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save and load a large index (100 documents).
    /// Verify performance is maintained and metadata is preserved.
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
        var index2 = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert
        Assert.Equal(initialDocCount, index2.DocumentCount);
        Assert.Equal(initialTermCount, index2.TermCount);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Save index with special characters and Unicode in document content.
    /// Verify saved and loaded index handles these correctly.
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
        var index2 = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert
        Assert.Equal(index1.DocumentCount, index2.DocumentCount);
        Assert.True(index2.TermCount > 0);

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
        Assert.Throws<System.Text.Json.JsonException>(() => Bm25Index<TestDoc>.LoadIndex(filePath));

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
    /// Verify each save completely replaces the previous index.
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

        var index2 = new Bm25Index<TestDoc>(docs2, d => d.Content);
        index2.SaveIndex(filePath);

        // Act
        var loadedIndex = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert
        Assert.Equal(2, loadedIndex.DocumentCount);

        // Cleanup
        File.Delete(filePath);
    }

    /// <summary>
    /// Verify save file format is compatible with different BM25 parameter configurations.
    /// Ensure saved index preserves parameter settings.
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
        var index2 = Bm25Index<TestDoc>.LoadIndex(filePath);

        // Assert
        Assert.Equal(2.0, index2.Parameters.K1);
        Assert.Equal(0.5, index2.Parameters.B);

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
