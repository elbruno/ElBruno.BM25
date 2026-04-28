# ElBruno.BM25 Test Strategy & Coverage Plan

## Phase 2 Deliverables: Test Design & Infrastructure

This document outlines the comprehensive testing strategy for ElBruno.BM25, designed to achieve **90%+ code coverage** and validate all aspects of the BM25 full-text search implementation.

---

## Test Project Structure

```
tests/ElBruno.BM25.Tests/
├── ElBruno.BM25.Tests.csproj       # xUnit test project (net8.0)
├── Bm25IndexTests.cs               # Core indexing & search (16 test cases)
├── TokenizerTests.cs               # Tokenization & text processing (10 test cases)
├── PersistenceTests.cs             # Serialization/deserialization (10 test cases)
├── PerformanceTests.cs             # Benchmarking & throughput (11 test cases)
├── EdgeCaseTests.cs                # Boundary conditions & edge cases (20 test cases)
└── Data/
    └── TestDocuments.cs            # Centralized test data sets
```

**Total Test Cases: 67 skeleton tests** ready for implementation in Phase 4.

---

## Test Framework & Dependencies

- **Framework:** xUnit 2.4.1 (most common in .NET ecosystem)
- **SDK:** Microsoft.NET.Test.Sdk 17.5.0
- **Runner:** xunit.runner.visualstudio 2.4.5
- **Target:** .NET 8.0 with Nullable reference types enabled

---

## Test Coverage Targets

### By Component

| Component | Target Coverage | Test File | Rationale |
|-----------|-----------------|-----------|-----------|
| Bm25Index<T> | 95% | Bm25IndexTests.cs | Core algorithm: full coverage required |
| Tokenizers | 90% | TokenizerTests.cs | Multiple implementations, various edge cases |
| Persistence | 90% | PersistenceTests.cs | Save/load critical for production use |
| Performance | 85% | PerformanceTests.cs | Benchmarks validate algorithmic efficiency |
| Edge Cases | 90% | EdgeCaseTests.cs | Robustness and error handling |

### Overall Goal: **90%+ code coverage**

Methods to verify coverage:
- Use `dotnet test` with OpenCover or built-in coverage analyzers
- Track before/after in each PR
- Quality gate: Coverage must not decrease

---

## Test Categories & Scope

### 1. Bm25IndexTests.cs (16 test cases)
**Focus:** Core algorithm correctness and search functionality

#### Basic Operations
- **TestBasicIndexing_SingleDocument** — Verify single document indexing
- **TestSearchBasic_ExactMatch** — Exact term matching
- **TestSearchBasic_NoMatch** — Empty results handling

#### Ranking & Relevance
- **TestSearchRanking_RelevanceSorting** — Term frequency affects ranking
- **TestSearchRanking_DocumentLengthNormalization** — B parameter controls normalization
- **TestSearchWithThreshold** — Threshold filtering works
- **TestSearchTopK_Limit** — Top-K parameter limits results

#### Parameter Tuning
- **TestParameterTuning_K1Variation** — K1 affects term frequency saturation
- **TestParameterTuning_BVariation** — B affects document length normalization

#### Batch & Dynamic Operations
- **TestBatchSearch_MultipleQueries** — Batch search efficiency
- **TestAddDocument_DynamicIndexing** — Dynamic document addition
- **TestRemoveDocument_DynamicIndexing** — Document removal
- **TestReindex_ReplaceAll** — Full reindex with new corpus

#### Multi-term & Edge Search
- **TestCaseInsensitivity_Search** — Case handling consistency
- **TestMultiTermQuery_PartialMatches** — Multiple term relevance
- **TestShortQueryHandling** — Very short queries

---

### 2. TokenizerTests.cs (10 test cases)
**Focus:** Text processing, stemming, and tokenization rules

#### Basic Tokenization
- **TestSimpleTokenizer_Lowercasing** — Case normalization
- **TestSimpleTokenizer_WhitespaceNormalization** — Whitespace handling
- **TestSimpleTokenizer_PunctuationHandling** — Punctuation rules

#### Advanced Tokenization (English Tokenizer)
- **TestEnglishTokenizer_PorterStemming** — Stemming correctness
- **TestEnglishTokenizer_StemmingWithStopwords** — Stopword removal + stemming
- **TestEnglishTokenizer_PluralNormalization** — Plural form normalization

#### Custom & Special Cases
- **TestCustomTokenizer_UserDefined** — Custom tokenizer logic
- **TestTokenizer_CaseInsensitivity** — Case-insensitive indexing/search
- **TestTokenizer_NumericHandling** — Number tokenization
- **TestTokenizer_UnicodeCharacters** — Unicode support (emoji, CJK, Arabic)

---

### 3. PersistenceTests.cs (10 test cases)
**Focus:** Index serialization, deserialization, and data integrity

#### Basic Save/Load
- **TestSaveIndex_CreatesFile** — File creation
- **TestLoadIndex_RestoresIndex** — Data integrity after save/load
- **TestLoadIndex_SearchWorks** — Functionality after load
- **TestSaveLoad_Roundtrip_Large** — 1000+ document round-trip

#### Special Cases
- **TestSaveLoad_UnicodeContent** — Unicode preservation
- **TestSaveLoad_ParameterPreservation** — BM25 parameters saved/restored
- **TestSaveLoad_EmptyIndex** — Empty index serialization

#### Error Handling
- **TestLoadIndex_CorruptedFile** — Corruption detection
- **TestLoadIndex_FileNotFound** — Missing file handling
- **TestSaveIndex_Overwrite** — Overwrite correctness

---

### 4. PerformanceTests.cs (11 test cases)
**Focus:** Throughput, latency, and scalability benchmarks

#### Indexing Performance
- **TestPerformance_Index100K_Under5s** — 100k docs in < 5s
- **TestPerformance_Index1M_Throughput** — 1M docs throughput validation

#### Search Performance
- **TestPerformance_Search_Under50ms** — Single-term search < 50ms
- **TestPerformance_MultiTermSearch_Latency** — Multi-term search overhead
- **TestPerformance_LargeTopK** — Large topK doesn't degrade

#### Batch & Scalability
- **TestPerformance_BatchSearch_Linear** — Batch scales linearly
- **TestPerformance_SaveLoad_100K** — Serialization speed
- **TestPerformance_IncrementalIndexing** — Dynamic add performance
- **TestPerformance_MemoryUsage** — Memory scaling (no leaks)
- **TestPerformance_RepeatedSearches** — Consistency across repeated calls
- **TestPerformance_ScalingBehavior** — Sublinear search latency scaling

---

### 5. EdgeCaseTests.cs (20 test cases)
**Focus:** Boundary conditions, error handling, and robustness

#### Empty/Null Cases
- **TestEdgeCase_EmptyIndex** — Empty index search
- **TestEdgeCase_SingleDocument** — Single-document edge case
- **TestEdgeCase_EmptyQuery** — Empty query string
- **TestEdgeCase_NullDocument** — Null document handling

#### Unicode & Encoding
- **TestEdgeCase_UnicodeCharacters** — Full Unicode support
- **TestEdgeCase_SpecialCharacters** — Special symbols (@, #, $, etc.)
- **TestEdgeCase_EncodingErrors** — Invalid UTF-8 handling

#### Size Extremes
- **TestEdgeCase_LongDocument** — 1MB+ documents
- **TestEdgeCase_HighCardinality** — 10k+ unique terms
- **TestEdgeCase_LongTerms** — 100+ character terms

#### Parameter & Config Edge Cases
- **TestEdgeCase_CaseSensitivity** — Case sensitivity modes
- **TestEdgeCase_TopKLargerThanCorpus** — topK > corpus size
- **TestEdgeCase_VeryHighThreshold** — Unreachable threshold
- **TestEdgeCase_InvalidThreshold** — Negative/zero threshold

#### Whitespace & Duplicates
- **TestEdgeCase_WhitespaceOnly** — Whitespace-only documents
- **TestEdgeCase_DuplicateTermsInQuery** — Duplicate terms in query
- **TestEdgeCase_DoubleRemove** — Removing same document twice
- **TestEdgeCase_DuplicateDocumentId** — Duplicate document IDs
- **TestEdgeCase_ReindexEmpty** — Reindex with empty set

---

## Test Data Strategy

### TestDocuments.cs Static Data Sets

#### 1. **Simple** (3 documents)
- Purpose: Basic relevance testing
- Content: Predictable term frequencies (3x, 2x, 1x "query")
- Usage: Ranking, scoring, threshold tests

#### 2. **WithLength** (2 documents)
- Purpose: Length normalization validation
- Content: Same term, vastly different lengths (short vs 1000+ words)
- Usage: B parameter tests, length normalization

#### 3. **WithUnicode** (4 documents)
- Purpose: Unicode edge case coverage
- Content: Emoji, Chinese, Arabic, mixed scripts
- Usage: Tokenizer Unicode tests, edge cases

#### 4. **WithSpecialCharacters** (4 documents)
- Purpose: Special character handling
- Content: Emails, URLs, numbers, symbols, code
- Usage: Tokenizer special char tests

#### 5. **LargeCorpus** (1000-1M documents)
- Purpose: Performance and scalability testing
- Content: Generated random realistic documents (fixed seed for reproducibility)
- Usage: Indexing benchmarks, search latency, scaling behavior

#### 6. **EdgeCases** (4 documents)
- Purpose: Boundary condition testing
- Content: Empty, whitespace-only, single word, 10k-word documents
- Usage: Edge case tests

#### 7. **CaseSensitivity** (3 documents)
- Purpose: Case handling validation
- Content: Lowercase, uppercase, mixed case versions of same terms
- Usage: Case sensitivity edge case tests

---

## Coverage Verification Strategy

### Phase 4 Implementation Plan

1. **Write implementations** for all 67 test cases (Gurney + Chani)
2. **Run coverage analysis:**
   ```bash
   dotnet test --configuration Release --collect:"XPlat Code Coverage"
   ```
3. **Generate coverage report** (OpenCover format)
4. **Track metrics per component** (see table above)
5. **Quality gate:** Block PRs if coverage drops below 90%

### Expected Coverage by Component

| Component | Estimated | Phase 4 Target |
|-----------|-----------|----------------|
| Bm25Index | 92-96% | > 95% |
| Tokenizers | 85-90% | > 90% |
| Persistence | 88-92% | > 90% |
| Utilities | 90-95% | > 90% |
| **Overall** | **~89%** | **> 90%** |

---

## Testing Best Practices

### Arrange-Act-Assert Pattern
All tests follow AAA pattern for clarity:
```csharp
[Fact]
public void TestName()
{
    // Arrange: Setup test data
    var index = new Bm25Index<TestDoc>(/* ... */);
    
    // Act: Execute the operation being tested
    var results = index.Search("query");
    
    // Assert: Verify expected outcome
    Assert.NotEmpty(results);
    Assert.True(results[0].Score > 0);
}
```

### Test Naming Convention
- `Test[Component]_[Scenario]_[ExpectedBehavior]`
- Example: `TestSearchRanking_DocumentLengthNormalization`

### Isolation & Reproducibility
- Each test is independent (no shared state)
- Random data uses fixed seeds (predictable results)
- Temporary files cleaned up (PersistenceTests)

### Performance Assertions
- Use `Stopwatch` for latency tests
- Assert on both time AND correctness
- Fail fast on threshold violations

---

## Success Criteria (Phase 4)

- ✅ All 67 test cases implemented
- ✅ All tests pass (green)
- ✅ Code coverage ≥ 90%
- ✅ Performance benchmarks met:
  - Index 100k docs in < 5 seconds
  - Search latency < 50ms
  - Batch search scales linearly
- ✅ No regressions from Phase 2/3
- ✅ Edge cases thoroughly validated
- ✅ Persistence round-trip verified

---

## Reviewer Checklist

Before approving code changes, verify:

- [ ] All new code has corresponding test(s)
- [ ] Tests are in correct file (by component)
- [ ] Test names clearly describe scenario
- [ ] Both happy path AND error cases tested
- [ ] Edge cases considered and handled
- [ ] Performance not degraded
- [ ] Code coverage maintained/increased
- [ ] All tests pass on main branch

---

## References

- **BM25 Specification:** TF-IDF with length normalization (K1, B parameters)
- **xUnit Documentation:** https://xunit.net/docs/getting-started/netcore
- **Coverage Tools:** OpenCover, Coverlet
- **Performance Benchmarking:** BenchmarkDotNet (future consideration)

---

## Appendix: Test Execution

### Run All Tests
```bash
cd C:\src\ElBruno.BM25
dotnet test tests/ElBruno.BM25.Tests/ElBruno.BM25.Tests.csproj -v normal
```

### Run Specific Test File
```bash
dotnet test tests/ElBruno.BM25.Tests/ElBruno.BM25.Tests.csproj --filter "FullyQualifiedName~Bm25IndexTests"
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --settings CodeCoverage.runsettings
```

### Generate Coverage Report
```bash
reportgenerator -reports:coverage.xml -targetdir:coveragereport -reporttypes:Html
```

---

**Document Version:** 1.0  
**Last Updated:** Phase 2 (Test Design)  
**Next Update:** Phase 4 (After implementation)
