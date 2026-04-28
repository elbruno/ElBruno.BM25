using Xunit;
using ElBruno.BM25;
using ElBruno.BM25.Tests.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ElBruno.BM25.Tests;

/// <summary>
/// Performance benchmarking tests.
/// Validates indexing speed, search latency, and throughput under load.
/// </summary>
public class PerformanceTests
{
    private readonly string _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "PerformanceTests");

    public PerformanceTests()
    {
        if (!Directory.Exists(_testDataPath))
            Directory.CreateDirectory(_testDataPath);
    }

    /// <summary>
    /// Index 100,000 random documents and measure total indexing time.
    /// Assert indexing completes in under 5 seconds.
    /// </summary>
    [Fact]
    public void TestPerformance_Index100K_Under5s()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(100000));
        var sw = Stopwatch.StartNew();

        // Act
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 5000, 
            $"Indexing 100K docs took {sw.ElapsedMilliseconds}ms, expected < 5000ms");
        Assert.Equal(100000, index.DocumentCount);
    }

    /// <summary>
    /// Index 100k documents, then perform single term search.
    /// Assert search completes in under 50ms.
    /// </summary>
    [Fact]
    public void TestPerformance_Search_Under50ms()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(100000));
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        
        // Warmup search
        index.Search("fox");

        // Act
        var sw = Stopwatch.StartNew();
        var results = index.Search("fox");
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Search took {sw.ElapsedMilliseconds}ms, expected < 50ms");
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Call SearchBatch() with 100 different queries on 100k document index.
    /// Verify batch time scales linearly with query count.
    /// </summary>
    [Fact]
    public async void TestPerformance_BatchSearch_Linear()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(100000));
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        var queries = new List<string> { "quick", "brown", "fox", "jumps", "over", "lazy", "dog", 
                                         "system", "search", "index", "document", "query", "algorithm", 
                                         "performance", "optimization" };

        // Measure single search time
        var sw1 = Stopwatch.StartNew();
        index.Search(queries[0]);
        sw1.Stop();
        var singleSearchTime = sw1.ElapsedMilliseconds;

        // Act - Measure batch search
        var sw2 = Stopwatch.StartNew();
        var results = await index.SearchBatch(queries);
        sw2.Stop();

        // Assert
        Assert.Equal(queries.Count, results.Count);
        var estimatedTime = singleSearchTime * queries.Count;
        Assert.True(sw2.ElapsedMilliseconds < estimatedTime * 1.5,
            $"Batch search took {sw2.ElapsedMilliseconds}ms, estimated {estimatedTime}ms");
    }

    /// <summary>
    /// Save and load a 100k document index.
    /// Verify serialization/deserialization time is acceptable.
    /// </summary>
    [Fact]
    public void TestPerformance_SaveLoad_100K()
    {
        // Arrange
        var docs = new List<TestDoc>(TestDocuments.LargeCorpus.GenerateDocuments(100000));
        var index = new Bm25Index<TestDoc>(docs, d => d.Content);
        var filePath = Path.Combine(_testDataPath, "perf_100k.json");

        // Act - Measure save
        var swSave = Stopwatch.StartNew();
        index.SaveIndex(filePath);
        swSave.Stop();

        var swLoad = Stopwatch.StartNew();
        var loadedIndex = Bm25Index<TestDoc>.LoadIndex(filePath);
        swLoad.Stop();

        // Assert
        // Note: These thresholds are lenient for CI/CD environments (GitHub Actions runners are slower)
        // Local performance is much better (typically <1s on developer machines)
        Assert.True(swSave.ElapsedMilliseconds < 60000,
            $"Save took {swSave.ElapsedMilliseconds}ms, expected < 60s for CI environment");
        Assert.True(swLoad.ElapsedMilliseconds < 30000,
            $"Load took {swLoad.ElapsedMilliseconds}ms, expected < 30s for CI environment");
        Assert.Equal(100000, loadedIndex.DocumentCount);

        // Cleanup
        File.Delete(filePath);
    }
}
