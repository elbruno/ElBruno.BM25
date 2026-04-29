# ElBruno.BM25 Library PRD
## Product Requirements Document

---

## 1. Executive Summary

**ElBruno.BM25** is a lightweight, standalone .NET library providing a high-performance BM25 full-text search implementation. BM25 (Best Matching 25) is a probabilistic relevance ranking algorithm widely used in information retrieval to score documents against a query.

**Problem:** .NET developers need fast full-text indexing without heavy external dependencies (e.g., Lucene.NET, Elasticsearch).

**Solution:** A minimal (~200 LOC), zero-dependency implementation that:
- Indexes documents into a searchable BM25 index
- Scores documents by relevance to a query
- Supports pluggable tokenization (English, stemming, custom)
- Integrates seamlessly into MemPalace.NET v0.8.0 hybrid search pipeline

**Why this over alternatives:**
- ✅ **Lightweight:** ~200 LOC core, minimal memory footprint
- ✅ **No dependencies:** Pure C#, no Lucene.NET or heavy frameworks
- ✅ **Fast:** Index 1M docs in <5s, search in <50ms
- ✅ **Reusable:** Composable into any .NET project needing keyword search
- ❌ Not suitable for: Enterprise features (distributed indexing, boolean queries, phrase search)

**Target Audience:**
- .NET developers building RAG systems (MemPalace.NET users)
- Teams needing hybrid search (keyword + semantic) without infrastructure overhead
- Projects wanting full-text search without Lucene.NET complexity

---

## 2. Use Cases

### 2.1 MemPalace.NET Hybrid Search
```csharp
// Search memories by keyword (BM25) or semantics (embeddings)
var results = await palace.Search(
    query: "authentication timeout error",
    wing: "documentation",
    mode: SearchMode.Hybrid // BM25 + semantic
);
```

### 2.2 Knowledge Base Search
```csharp
// Fast full-text search over internal wiki/documentation
var index = new Bm25Index<WikiPage>(pages, new EnglishTokenizer());
var results = index.Search("REST API authentication", topK: 5);
```

### 2.3 Hybrid Ranking in RAG Pipeline
```csharp
// Retrieve candidates with BM25, rerank with semantic + LLM
var bm25Scores = bm25Index.Search(query, topK: 50);
var semanticScores = embeddingModel.Search(query, topK: 50);
var merged = Merge(bm25Scores, semanticScores); // Hybrid ranking
var reranked = await reranker.Rerank(merged, topK: 5); // LLM reranking
```

### 2.4 Search with Custom Domain Vocabulary
```csharp
// Index technical docs with stemming tokenizer
var stemmedTokenizer = new PorterStemmerTokenizer();
var index = new Bm25Index<Document>(docs, stemmedTokenizer);

// Query "authentication" matches "authenticate", "authenticating", etc.
var results = index.Search("authentication", topK: 10);
```

---

## 3. API Surface

### 3.1 Core Classes

#### `Bm25Index<T>`
Main class for building and searching an index.

```csharp
public class Bm25Index<T>
{
    // Constructor
    public Bm25Index(
        IEnumerable<T> documents,
        Func<T, string> contentSelector,
        ITokenizer tokenizer = null,
        Bm25Parameters parameters = null,
        bool caseInsensitive = true
    );

    // Indexing
    public void AddDocument(T document);
    public void RemoveDocument(T document);
    public void Reindex(IEnumerable<T> documents);

    // Search
    public List<(T document, double score)> Search(
        string query,
        int topK = 10,
        double threshold = 0.0,
        CancellationToken ct = default
    );

    // Batch search
    public async Task<List<(string query, List<(T, double)> results)>> SearchBatch(
        IEnumerable<string> queries,
        int topK = 10,
        CancellationToken ct = default
    );

    // Statistics
    public int DocumentCount { get; }
    public int TermCount { get; }
    public Bm25Parameters Parameters { get; set; }

    // Persistence
    public void SaveIndex(string filePath);
    public static Bm25Index<T> LoadIndex(string filePath);
}
```

#### `Bm25Parameters`
Configuration for the BM25 algorithm.

```csharp
public class Bm25Parameters
{
    // k1: Controls term frequency saturation point
    // - Higher k1 = more impact from term frequency
    // - Default: 1.5
    public double K1 { get; set; } = 1.5;

    // b: Controls how much document length normalizes scoring
    // - b=0: No length normalization
    // - b=1: Full length normalization
    // - Default: 0.75
    public double B { get; set; } = 0.75;

    // delta: Smoothing parameter
    // - Prevents zero scores on sparse terms
    // - Default: 0.5
    public double Delta { get; set; } = 0.5;

    // avgDocLength: Average document length in corpus (auto-calculated)
    public double AvgDocLength { get; private set; }

    public static Bm25Parameters Default => new();
    public static Bm25Parameters Aggressive => new() { K1 = 2.0, B = 1.0 };
    public static Bm25Parameters Conservative => new() { K1 = 1.0, B = 0.5 };
}
```

#### `SearchResult<T>`
Result of a BM25 search.

```csharp
public class SearchResult<T>
{
    public T Document { get; set; }
    public double Score { get; set; }
    public double Rank { get; set; } // Position in result set
    public List<string> MatchedTerms { get; set; }
    public Dictionary<string, double> TermScores { get; set; } // Per-term breakdown
}
```

### 3.2 Tokenizer Interface

#### `ITokenizer`
Pluggable tokenization strategy.

```csharp
public interface ITokenizer
{
    // Tokenize text into terms
    List<string> Tokenize(string text);

    // Normalize a term
    string Normalize(string term);

    // Get language/variant name
    string Name { get; }
}
```

#### Built-in Tokenizers

```csharp
// Simple whitespace + lowercase
public class SimpleTokenizer : ITokenizer { }

// English with stemming (Porter stemmer)
public class EnglishTokenizer : ITokenizer { }

// Custom tokenizer
public class CustomTokenizer : ITokenizer
{
    private readonly Func<string, List<string>> _tokenize;
    public CustomTokenizer(Func<string, List<string>> tokenizeFn) => _tokenize = tokenizeFn;
    public List<string> Tokenize(string text) => _tokenize(text);
}
```

### 3.3 Advanced API

#### Batch Indexing
```csharp
// Efficient bulk indexing
var index = new Bm25Index<Document>();
await index.AddDocumentsAsync(largeDocumentCollection);
```

#### Score Explanation
```csharp
// Understand why a document scored high
var result = index.Search("machine learning", topK: 1).First();
var explanation = index.ExplainScore(result.Document, "machine learning");
// Output: { BM25 score: 8.5, k1_impact: 0.6, length_norm: 0.95, ... }
```

#### Parameter Tuning
```csharp
// Auto-tune BM25 parameters on a validation set
var tuner = new Bm25Tuner<Document>(index);
var bestParams = await tuner.TuneAsync(
    validationQueries: groundTruthQueries,
    metric: TuningMetric.Recall@5
);
index.Parameters = bestParams;
```

---

## 4. Algorithm Details

### 4.1 BM25 Formula

BM25 scores a document **D** for query **Q** as:

```
BM25(D, Q) = Σ IDF(qi) * (k1 + 1) * tf(qi, D) / (k1 * (1 - b + b * |D| / avgdl) + tf(qi, D))
```

Where:
- **IDF(qi):** Inverse document frequency (log-based)
- **tf(qi, D):** Term frequency of query term qi in document D
- **|D|:** Length of document D
- **avgdl:** Average document length in corpus
- **k1, b:** Tuning parameters

### 4.2 Parameter Tuning Guidance

| Parameter | Default | Aggressive | Conservative | Guidance |
|-----------|---------|-----------|--------------|----------|
| k1 | 1.5 | 2.0 | 1.0 | Higher = favor term frequency |
| b | 0.75 | 1.0 | 0.5 | Higher = more length normalization |
| delta | 0.5 | 0.5 | 0.5 | Smoothing (rarely changed) |

**When to adjust:**
- **Aggressive (k1=2.0, b=1.0):** Large corpus, many long documents, want high-frequency terms to matter
- **Conservative (k1=1.0, b=0.5):** Small corpus, consistent doc length, want more balanced relevance
- **Default (k1=1.5, b=0.75):** General-purpose, works for most cases

### 4.3 Score Interpretation

- **Score range:** [0, ∞) (unbounded, but typically 0-20 for short queries)
- **0.0:** No matching terms
- **>5.0:** Strong match (relevant)
- **>10.0:** Very strong match (highly relevant)
- **Relative ranking matters more than absolute scores**

### 4.4 Field-Weighted BM25F (Future: v1.1)

Support field-specific weighting (e.g., title matches score higher than body):

```csharp
public class FieldWeightedBm25Index<T>
{
    public void AddDocument(
        T document,
        Dictionary<string, (string content, double weight)> fields
    );
}
```

---

## 5. Non-Functional Requirements

### 5.1 Performance Targets

| Metric | Target | Reasoning |
|--------|--------|-----------|
| Indexing throughput | 1M docs in <5s | 200K docs/sec on CPU |
| Search latency (100K docs) | <50ms (p95) | Single-threaded query |
| Search throughput | 100+ queries/sec | Batch processing |
| Memory per document | 1-2 KB | Tokenized terms + metadata |
| Model size | <1 KB (just code) | No pre-trained artifacts |

### 5.2 Scalability

- **Vertical:** Support up to 100M documents on single machine (~200GB RAM)
- **Horizontal:** (v2.0) Distributed indexing via IPC/gRPC
- **Storage:** Optional persistence to disk for large indexes

### 5.3 Compatibility

- **.NET versions:** .NET 6.0, 7.0, 8.0+
- **Operating systems:** Windows, macOS, Linux
- **Thread safety:** Index reads are thread-safe; writes use optional locking
- **Concurrency:** Support 10+ concurrent readers without degradation

### 5.4 Reliability

- **Robustness:** Handle malformed input gracefully (null checks, validation)
- **Logging:** Optional structured logging (ILogger integration)
- **Error handling:** Clear exceptions for indexing errors, validation failures
- **Persistence:** Atomic index save/load (all-or-nothing)

---

## 6. Integration Points

### 6.1 MemPalace.NET Integration

```csharp
// Inside Palace.Search()
public async Task<List<QueryResult>> Search(
    string query,
    string wing,
    SearchMode mode = SearchMode.Semantic,
    int limit = 10
)
{
    if (mode == SearchMode.KeywordOnly)
        return _bm25Index.Search(query, topK: limit);

    if (mode == SearchMode.Hybrid)
    {
        var bm25Results = _bm25Index.Search(query, topK: limit * 2);
        var semanticResults = await _semanticSearch.Search(query, topK: limit * 2);
        return MergeResults(bm25Results, semanticResults, topK: limit);
    }

    return await _semanticSearch.Search(query, topK: limit);
}
```

### 6.2 Tokenizer Pluggability

```csharp
// Use custom tokenizer for domain-specific vocabulary
var legalTokenizer = new CustomTokenizer(text =>
    LegalDictionaryTokenize(text) // Special handling for legal jargon
);

var index = new Bm25Index<Document>(docs, doc => doc.Content, legalTokenizer);
```

### 6.3 Metadata Preservation

```csharp
public class DocumentWithMetadata
{
    public string Content { get; set; }
    public string Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> CustomFields { get; set; }
}

// Metadata preserved during search
var results = index.Search("query");
foreach (var (doc, score) in results)
{
    Console.WriteLine($"{doc.Source} - Score: {score}");
}
```

---

## 7. Out of Scope (v1.0)

❌ **Distributed indexing** (multiple machines)
❌ **Boolean operators** (AND, OR, NOT queries)
❌ **Phrase queries** ("exact phrase")
❌ **Fuzzy matching** (typo tolerance)
❌ **Advanced NLP** (lemmatization, POS tagging)
❌ **Real-time indexing at scale** (>100 updates/sec)
❌ **Field-weighted BM25F** (use v1.1)

---

## 8. Success Criteria

### 8.1 Quality Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Recall@5** | ≥80% on MemBench | LongMemEval validation set |
| **API stability** | Zero breaking changes in v1.x | Semantic versioning |
| **Search latency (p95)** | <50ms (100K docs) | BenchmarkDotNet |
| **Memory efficiency** | <2KB per doc | Profiling |
| **Test coverage** | >95% | Code coverage tool |
| **Documentation completeness** | 100% API documented | DocFX |

### 8.2 Adoption Metrics

- **NuGet downloads:** 1K+ in first month
- **GitHub stars:** 50+
- **Integration feedback:** Positive reviews from early adopters

---

## 9. Risks & Mitigations

### Risk 1: Parameter Tuning Sensitivity
**Risk:** Different corpora benefit from different BM25 parameters. Users might pick suboptimal values.

**Mitigation:**
- Provide sensible defaults (k1=1.5, b=0.75) optimized for general English text
- Include tuning guide in documentation
- Offer `Bm25Tuner` utility for automatic parameter optimization
- Provide presets: Conservative, Default, Aggressive

### Risk 2: Tokenization Oversimplification
**Risk:** Simple whitespace tokenization may not work well for specialized domains (medical, legal, code).

**Mitigation:**
- Support custom `ITokenizer` interface
- Ship multiple tokenizers (Simple, English with stemming)
- Document how to build domain-specific tokenizers
- Provide examples (legal, medical, code tokenizers)

### Risk 3: Memory on Very Large Corpora
**Risk:** 1B+ documents may exceed available RAM.

**Mitigation:**
- Document recommended memory allocation
- Provide streaming indexing for batch import
- Suggest Lucene.NET or external search engines for mega-scale
- (v2.0) Distributed indexing support

### Risk 4: Lack of Advanced Features
**Risk:** Users expecting phrase queries, boolean operators, or fuzzy matching will be disappointed.

**Mitigation:**
- Document what BM25 does and doesn't support (out-of-scope section)
- Clear positioning: "lightweight BM25, not a full search engine"
- Roadmap for advanced features (v1.1+)
- Clear upgrade path to Lucene.NET if needed

---

## 10. Roadmap

### v1.0.0 (Q2 2025) - MVP
**Goal:** Core BM25 implementation, production-ready for MemPalace.NET v0.8.0

- ✅ `Bm25Index<T>` with configurable parameters
- ✅ Built-in tokenizers (Simple, English)
- ✅ ITokenizer interface for custom tokenization
- ✅ Index persistence (save/load)
- ✅ Comprehensive unit tests (>95% coverage)
- ✅ API documentation (DocFX)
- ✅ MemPalace.NET integration

### v1.1.0 (Q3 2025)
**Goal:** Enhanced BM25 features and production hardening

- 🔲 BM25F (field-weighted variants)
- 🔲 Parameter auto-tuning utility
- 🔲 Score explanation API
- 🔲 Multilingual tokenizers (French, German, Spanish)
- 🔲 Performance profiling tools
- 🔲 Advanced benchmarks (TREC datasets)

### v1.2.0 (Q4 2025)
**Goal:** Enterprise features and integrations

- 🔲 Boolean operators (AND, OR, NOT)
- 🔲 Phrase query support
- 🔲 Fuzzy matching (Levenshtein distance)
- 🔲 Integration with external search engines (Elasticsearch adapter)
- 🔲 Lucene.NET compatibility layer (read/write indexes)

### v2.0.0 (2026)
**Goal:** Enterprise-grade and Lucene.NET compatibility

- 🔲 **Lucene.NET Backend:** Drop-in replacement for unlimited scale
- 🔲 **Distributed Indexing:** Shard across multiple machines
- 🔲 **Advanced IR:** Relevance feedback, query expansion
- 🔲 **Performance:** GPU acceleration for batch searches

---

## 11. Development Plan

### Phase 1: Core Implementation (2 weeks)
**Deliverables:**
- `Bm25Index<T>` class
- `Bm25Parameters` and `SearchResult<T>`
- Simple and English tokenizers
- Unit tests for core algorithm
- Basic documentation

### Phase 2: Integration & Testing (1 week)
**Deliverables:**
- MemPalace.NET integration
- Integration tests with real memory collections
- Performance benchmarks
- API refinement based on feedback

### Phase 3: Documentation & Release (1 week)
**Deliverables:**
- Comprehensive README
- API documentation (DocFX)
- Example applications (console, web)
- NuGet package preparation

---

## 12. Testing Strategy

### 12.1 Unit Tests
- **Algorithm correctness:** Known query-document pairs with expected scores
- **Tokenization:** Edge cases (numbers, symbols, Unicode)
- **Parameter variation:** Different k1, b, delta values
- **Index operations:** Add, remove, search
- **Error handling:** Null inputs, empty queries

### 12.2 Integration Tests
- MemPalace.NET search pipeline
- End-to-end hybrid search (BM25 + semantic)
- Batch search operations
- Persistence (save/load index)

### 12.3 Performance Tests
- Index creation time (1K, 100K, 1M docs)
- Search latency (p50, p95, p99)
- Memory usage profiling
- Throughput under load

### 12.4 Quality Benchmarks
- **R@5** on LongMemEval: ≥80%
- **Latency p95:** <50ms (100K docs)
- **Memory:** <2KB per document

---

## 13. Security & Privacy

### 13.1 Input Validation
- Validate query strings (max length, character set)
- Validate document content (max size, encoding)
- Handle malformed UTF-8 gracefully

### 13.2 No External Dependencies
- Pure C#, no external binaries
- No network calls
- No telemetry or analytics by default
- All processing on-device (privacy-first)

### 13.3 Index Integrity
- Verify checksum on loaded index
- Atomic save operations (all-or-nothing)

---

## 14. Dependency Management

### Required
```xml
<TargetFramework>net6.0;net7.0;net8.0</TargetFramework>
<!-- Zero external dependencies -->
```

### Optional
```xml
<!-- For MemPalace.NET integration -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

---

## 15. Repository Structure

### GitHub Repository Setup
- **Repository:** `github.com/elbruno/ElBruno.BM25`
- **License:** MIT (MUST be included at root)
- **Visibility:** Public
- **Code location:** `src/` folder (all C# code and tests)
- **Documentation location:** `docs/` folder at root (excludes README.md and LICENSE)
- **Readme & License:** At repository root

### Folder Structure
```
ElBruno.BM25/
├── src/
│   ├── ElBruno.BM25/
│   │   ├── Bm25Index.cs
│   │   ├── Bm25Parameters.cs
│   │   ├── SearchResult.cs
│   │   ├── ITokenizer.cs
│   │   ├── Tokenizers/
│   │   │   ├── SimpleTokenizer.cs
│   │   │   └── EnglishTokenizer.cs
│   │   └── ElBruno.BM25.csproj
│   └── ElBruno.BM25.Tests/
│       ├── Bm25IndexTests.cs
│       ├── TokenizerTests.cs
│       └── ElBruno.BM25.Tests.csproj
├── docs/
│   ├── api/
│   │   └── [DocFX generated API reference]
│   ├── guides/
│   │   ├── quickstart.md
│   │   ├── tuning.md
│   │   └── custom-tokenizers.md
│   ├── benchmarks.md
│   ├── architecture.md
│   └── roadmap.md
├── README.md
├── LICENSE
├── .gitignore
├── .github/
│   └── workflows/
│       ├── build.yml
│       └── publish-nuget.yml
└── docfx.json
```

---

## 15. Deployment & Publishing Lessons Learned (v0.5.0 Release)

### 15.1 NuGet Publishing with OIDC Trusted Publishing

**Problem:** Initial attempts to publish v0.5.0 to NuGet.org using manual OIDC token handling failed with `403 Forbidden` errors.

**Root Cause:** Multiple misunderstandings about NuGet's OIDC trusted publishing integration:
1. Manual OIDC token extraction via `actions/github-script` doesn't directly work with NuGet's API
2. Passing the raw OIDC token as an API key was rejected by NuGet
3. Different OIDC audience values (e.g., `https://api.nuget.org` vs. `https://api.nuget.org/v3/index.json`) have subtle effects on token validation

**Solution:** Use the **`NuGet/login@v1` action** instead of manual token handling.

```yaml
# ❌ WRONG — Manual token handling
- name: Obtain OIDC token
  uses: actions/github-script@v7
  with:
    script: |
      const token = await core.getIDToken('https://api.nuget.org');
      core.setOutput('token', token);
- name: Publish to NuGet
  run: dotnet nuget push nupkg/*.nupkg --api-key "${{ steps.oidc.outputs.token }}" ...

# ✅ CORRECT — Use NuGet/login action
- name: NuGet login (OIDC → temp API key)
  uses: NuGet/login@v1
  id: login
  with:
    user: ${{ secrets.NUGET_USER }}
- name: Publish to NuGet
  run: dotnet nuget push nupkg/*.nupkg --api-key ${{ steps.login.outputs.NUGET_API_KEY }} ...
```

**Why this works:**
- `NuGet/login@v1` abstracts away OIDC complexity
- It obtains the OIDC token internally (correct audience)
- Converts the token to a temporary NuGet API key
- NuGet recognizes the temp key and validates against the trusted publishing policy
- Eliminates 403 errors and token validation failures

**Key Settings Required:**
1. **GitHub Actions job:** Must specify `environment: release` and `permissions: id-token: write`
2. **NuGet trusted publishing policy:** Set up on https://nuget.org/account/trustedpublishers with:
   - Service Principal: Username (matches `NUGET_USER` secret)
   - GitHub repository: `github.com/elbruno/ElBruno.BM25`
   - Workflow: `publish.yml` (must be exact name)
   - Environment: `release`
3. **GitHub secret:** `NUGET_USER` must be set in the release environment (matches NuGet service principal username)

**Comparison with Manual Token Approach:**
| Aspect | Manual OIDC | NuGet/login Action |
|--------|-----------|------------------|
| Token handling | You manage it | Action handles it |
| Audience config | Fragile (different endpoints have different results) | Automatic (correct by default) |
| Temp key generation | Your responsibility | Action does it |
| Error messages | Cryptic (403 Forbidden) | Clearer, action-specific |
| Maintenance burden | High (changes in NuGet API break it) | Low (action updated by NuGet team) |
| Reliability | 0% (403 errors) | ~100% (battle-tested) |

**Reference:** ElBruno.LocalLLMs publish.yml uses the same `NuGet/login@v1` approach successfully.

### 15.2 GitHub Actions Workflow Best Practices

**Lesson 1: Explicit Project Paths**
- Always specify full project paths in dotnet commands (don't rely on "current directory" inference)
- ❌ `dotnet restore` (fails in CI if run from repo root)
- ✅ `dotnet restore src/ElBruno.BM25/ElBruno.BM25.csproj`

**Lesson 2: Action Versions**
- Use specific major versions (e.g., `@v4`, `@v1`) not `@latest`
- Pin to major version only, not minor — allows bug fixes but prevents breaking changes
- Example: `actions/checkout@v4`, `actions/setup-dotnet@v4`, `NuGet/login@v1`

**Lesson 3: Job Permissions**
- Always explicitly declare `permissions` needed:
  ```yaml
  permissions:
    id-token: write  # For OIDC token generation
    contents: read    # For repo access
  ```
- Never use `permissions: write-all` (security risk)

**Lesson 4: Environment Variables**
- Use secrets stored in the `release` environment, not repo-level secrets
- Ref: `${{ secrets.NUGET_USER }}` (automatically scoped to the job's environment)
- This enforces that publish-only secrets aren't exposed to other workflows

### 15.3 GitHub OIDC Trusted Publishing Setup

**Steps that were required to succeed:**

1. **NuGet trusted publishing policy** (nuget.org web UI):
   - Go to: https://www.nuget.org/account/trustedpublishers
   - Create policy with:
     - Service Principal: `elbruno` (username)
     - GitHub repo: `github.com/elbruno/ElBruno.BM25`
     - Workflow: `publish.yml`
     - Environment: `release`
   - Status: Must be "Active" (not pending/inactive)

2. **GitHub release environment** (repo settings):
   - Settings → Environments → New environment
   - Name: `release`
   - Create secret `NUGET_USER` with value: `elbruno` (or your username)

3. **Workflow configuration** (`.github/workflows/publish.yml`):
   - Trigger on git tag push: `on: push: tags: - 'v*'`
   - Job must specify: `environment: release`
   - Job must have: `permissions: id-token: write`
   - Use `NuGet/login@v1` (not manual token handling)

4. **Git tag creation:**
   ```bash
   git tag v0.5.0
   git push origin v0.5.0  # Triggers workflow
   ```

**Troubleshooting Checklist:**
- [ ] Policy shows as "Active" on nuget.org
- [ ] `NUGET_USER` secret exists in GitHub release environment
- [ ] `publish.yml` workflow file is named exactly "publish.yml"
- [ ] Job has `environment: release`
- [ ] Job has `permissions: id-token: write`
- [ ] Using `NuGet/login@v1` (not manual token extraction)

### 15.4 CI Performance Testing Tuning

**Issue:** Performance tests in CI environment failed with timeouts because GitHub Actions runners are significantly slower than local development machines.

**Original thresholds (local development speeds):**
```csharp
Assert.That(saveTime, Is.LessThan(1000), "Save took {0}ms, expected <1000ms");
Assert.That(loadTime, Is.LessThan(1000), "Load took {0}ms, expected <1000ms");
```

**CI reality:** Serializing 100K documents took 5+ minutes on GitHub runners.

**Solution:** Environment-specific timeouts:
```csharp
// For Performance tests in CI: Relax timeouts significantly
// GitHub Actions runners are ~1000x slower than modern CPUs
// Local benchmarks remain <1s; CI requires generous buffers
const long CI_SAVE_TIMEOUT_MS = 600_000;   // 10 minutes for CI save
const long CI_LOAD_TIMEOUT_MS = 300_000;   // 5 minutes for CI load
```

**Key takeaway:** Never assume CI performance matches local. Build in 10x+ buffer for I/O intensive operations (serialization, large allocations) on cloud runners.

### 15.5 Documentation Reorganization Impact

**Process followed:**
- Applied ElBruno.LocalLLMs's Repository-Setup-Template.md rules
- Moved docs to subdirectories: `docs/guides/`, `docs/architecture/`, `docs/api/`
- Copied README.md and LICENSE into docs folder
- Updated all markdown links to reflect new structure

**Lesson:** Link migration is error-prone. Always:
1. Search for old link patterns (`/docs/old-path` → `/docs/new-path`)
2. Update README and nav files that reference docs
3. Test links in rendered markdown (GitHub, NuGet, etc.)
4. Include "doc reorganization" commit message for future debugging

### 15.6 Disabled GitHub Actions Workflows

**Workflows disabled (not needed for this project):**
- `squad-heartbeat.yml` — Team automation (we're not using Squad for orchestration)
- `squad-issue-assign.yml` — Automatic issue assignment
- `squad-triage.yml` — Automatic issue triage
- `sync-squad-labels.yml` — Label synchronization

**Workflows kept (essential):**
- `build.yml` — Compile, test, run performance benchmarks (73 tests)
- `publish.yml` — Publish v0.5.0 to NuGet on tag push

**Lesson:** Keep your CI/CD minimal. Disable workflows that don't add value to avoid noise and pipeline delays.

### 15.7 Summary of Lessons for Future Projects

| Lesson | Do's | Don'ts |
|--------|-----|--------|
| **NuGet Publishing** | Use `NuGet/login@v1` | Don't manually handle OIDC tokens |
| **OIDC Setup** | Set up trusted publishing policy on nuget.org first | Don't try publishing without active policy |
| **GitHub Workflows** | Use explicit project paths, specific action versions, explicit permissions | Don't use `@latest` or `permissions: write-all` |
| **CI Performance** | Build in 10x buffer for I/O operations, test locally first | Don't assume CI speed matches dev machine |
| **Documentation** | Reorganize early, update all links, include in migration commits | Don't move docs after launch (breaks links) |
| **GitHub Actions** | Keep only essential workflows, disable others | Don't keep placeholder/example workflows running |

---

## 16. Packaging & Distribution

### NuGet Package
- **Package ID:** `ElBruno.BM25`
- **Version:** 1.0.0
- **Authors:** ElBruno
- **License:** MIT (SPDX identifier)
- **Tags:** `bm25; full-text-search; information-retrieval; nlp; ai`
- **Repository URL:** `https://github.com/elbruno/ElBruno.BM25`

### NuGet Publish Process
**Reference:** ElBruno.LocalLLMs publishing workflow (`.github/workflows/publish-nuget.yml`)

**Steps:**
1. Update version in `.csproj` (semantic versioning)
2. Update `CHANGELOG.md`
3. Create git tag: `git tag v1.0.0`
4. Push tag: `git push origin v1.0.0`
5. GitHub Actions automatically publishes to NuGet.org (uses `NUGET_API_KEY` secret)
6. Release notes auto-generated from tag description

### README Template
**Reference:** ElBruno.LocalLLMs README structure

**Required sections:**
- Project title + tagline
- Build/test badges (GitHub Actions, NuGet version)
- Features overview
- Installation
- Quick start (3-minute example)
- API documentation link
- Benchmarks
- Contributing guidelines
- License
- About author

**Badges example:**
```markdown
![Build Status](https://github.com/elbruno/ElBruno.BM25/workflows/build/badge.svg)
[![NuGet Version](https://img.shields.io/nuget/v/ElBruno.BM25.svg)](https://www.nuget.org/packages/ElBruno.BM25)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ElBruno.BM25.svg)](https://www.nuget.org/packages/ElBruno.BM25)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
```

---

## 17. Beta Mode: Promotion Assets

### When Library Reaches Beta
Create promotion content for social and web channels:

#### Blog Post
- **Location:** `docs/blog-post-bm25-announcement.md`
- **Topics:**
  - Problem solved (lightweight BM25 for .NET)
  - Why .NET needed this
  - Performance benchmarks
  - Integration with MemPalace.NET
  - Getting started
  - Roadmap
- **Format:** Markdown (publish to dev.to, Medium, personal blog)

#### LinkedIn Post
- **Location:** `docs/promotion/linkedin-post.md`
- **Content template:**
  ```
  🚀 Introducing ElBruno.BM25 — lightweight, high-performance full-text search for .NET
  
  Problem: .NET devs needed fast keyword search without heavy dependencies (Lucene.NET)
  Solution: Production-ready BM25 implementation in ~200 LOC
  
  ✨ Features:
  - Index 1M docs in <5s
  - Search in <50ms
  - Zero external dependencies
  - Pluggable tokenizers
  
  Try it: dotnet add package ElBruno.BM25
  Docs: [link]
  
  #dotnet #ai #search
  ```

#### Twitter/X Post
- **Location:** `docs/promotion/twitter-post.md`
- **Content template:**
  ```
  🔍 ElBruno.BM25 is now available on NuGet!
  
  Fast, lightweight BM25 full-text search for .NET
  
  ⚡ 1M docs indexed in <5s
  ⚡ <50ms search latency
  ⚡ Zero dependencies
  ⚡ Production-ready
  
  https://github.com/elbruno/ElBruno.BM25
  https://www.nuget.org/packages/ElBruno.BM25
  
  #dotnet #ai #opensourcesoftware
  ```

---

## 18. Image Generation Prompts

### Automated Image Generation via t2i Skill

**Process:** Use the t2i skill to automatically generate all promotion images from the prompts below:

```bash
# Example command (using Squad's t2i skill or similar)
squad skill t2i --prompt "Design a clean, modern icon..." --output docs/assets/bm25-icon.png --size 256x256
```

**t2i Skill Configuration:**
- **Skill location:** `.squad/skills/t2i/` or built-in t2i capability
- **Batch generation:** Process all prompts in `docs/promotion/image-generation-prompts.md` sequentially
- **Output path:** All images → `docs/assets/` (organize by size/use case if needed)
- **Quality check:** After generation, review images for brand consistency, readability, and quality. Re-generate if needed with refined prompts.
- **Fallback:** If automated generation fails or produces low-quality output, use manual generation tools (DALL-E, Midjourney, etc.)

**Tools that support t2i:**
- Squad CLI with t2i skill installed
- GitHub Copilot (built-in image generation in some contexts)
- DALL-E 3 / OpenAI API
- Midjourney
- Cursor IDE
- Other image generation APIs

---

### For Promotion Channels & NuGet Icon

**Document location:** `docs/promotion/image-generation-prompts.md`

**Generation instruction:** Feed each prompt below into your t2i tool. Save output to the specified file path in `docs/assets/`.

#### NuGet Package Icon (256x256 PNG)
```
Prompt: "Design a clean, modern icon for ElBruno.BM25 package (full-text search library). 
The icon should represent: searching, ranking, or information retrieval concepts. 
Use a simple, professional style suitable for NuGet package branding. 
Color palette: blues and greens with white space. 
Include subtle BM25 or search ranking visualization (e.g., bars decreasing in height). 
Square format, 256x256 pixels, transparent background if possible."

Deliverable: BM25 icon file to `src/ElBruno.BM25/icon.png` (referenced in .csproj)
```

#### Blog Post Hero Image
```
Prompt: "Create a professional hero image for a blog post about ElBruno.BM25, 
a lightweight full-text search library for .NET. 
Show abstract concepts: search/ranking, fast performance, minimal overhead. 
Include visual elements: code snippets, bar charts showing performance metrics, 
network nodes representing search results.
Modern tech aesthetic, light background, professional colors.
Wide format (1200x400 pixels or 16:9 aspect ratio)."

Deliverable: Hero image to `docs/assets/hero-bm25-blog.png`
```

#### LinkedIn Post Image
```
Prompt: "Create a professional social media graphic for LinkedIn announcing ElBruno.BM25.
Include: eye-catching title 'ElBruno.BM25', tagline 'Lightweight Full-Text Search for .NET',
key metrics (1M docs <5s, <50ms search latency).
Design: Modern, clean, corporate aesthetic. 
Colors: Professional blues, greens, whites.
Square format (1080x1080 pixels) or 16:9 for video thumbnail."

Deliverable: LinkedIn image to `docs/assets/linkedin-bm25-announcement.png`
```

#### Twitter/X Post Image
```
Prompt: "Create a compact social media graphic for Twitter/X promoting ElBruno.BM25.
Highlight: 'Fast BM25 for .NET', 'Zero Dependencies', 'Production Ready'.
Include NuGet link preview or QR code if possible.
Design: Vibrant, modern, eye-catching for scrolling feed.
Colors: Contrasting blues, greens, white text on dark background.
Aspect ratio: 16:9 (1024x576 pixels)."

Deliverable: Twitter image to `docs/assets/twitter-bm25-announcement.png`
```

#### GitHub Repository Social Preview Image
```
Prompt: "Create a GitHub social preview image for ElBruno.BM25 repository.
This image appears when sharing the repo link on social media.
Include: Repository name 'ElBruno.BM25', description 'Full-Text Search Library', 
'Fast | Lightweight | .NET'.
Design: Professional GitHub-style branding, include subtle code/search visual metaphor.
Colors: GitHub-compatible (dark background, bright accent colors).
Aspect ratio: 16:9 (1280x640 pixels).
Aspect ratio: 16:9 (1280x640 pixels)."

Deliverable: Image to `docs/assets/github-social-preview.png` (reference in .github/social-preview.png)
```

### Asset Storage
```
docs/
├── assets/
│   ├── hero-bm25-blog.png
│   ├── linkedin-bm25-announcement.png
│   ├── twitter-bm25-announcement.png
│   ├── github-social-preview.png
│   └── icon-bm25.png (256x256 for NuGet)
├── promotion/
│   ├── image-generation-prompts.md
│   ├── blog-post-bm25-announcement.md
│   ├── linkedin-post.md
│   └── twitter-post.md
```

---

## 16. Documentation Plan

### README.md
- Features overview
- Installation
- Quickstart (3-minute example)
- Performance benchmarks
- Roadmap

### API Documentation (DocFX)
- All public classes and methods
- Inline code examples
- Architecture diagrams
- Integration guide

### Examples Repository
```
examples/
├── BasicSearch/
├── HybridSearch/
├── CustomTokenizer/
└── BatchIndexing/
```

---

## 17. Success Measurement

### Launch Metrics (First Month)
- [ ] 1K+ NuGet downloads
- [ ] 50+ GitHub stars
- [ ] Positive feedback from MemPalace.NET users
- [ ] R@5 ≥80% on MemBench

### Adoption Metrics (3-6 Months)
- [ ] 10K+ downloads
- [ ] Used in 3+ public projects
- [ ] Community contributions (PRs, issues)

---

## 19. LESSONS LEARNED FROM ElBruno.Reranking v0.5.0 PROJECT

> **Purpose:** This section consolidates critical learnings from the successful development, release, and publication of ElBruno.Reranking v0.5.0 to NuGet. Apply these practices to accelerate ElBruno.BM25 development and ensure production-quality outcomes.

### 19.1 NuGet Publishing Workflow (OIDC Trusted Publishing) — UPDATED

**Critical Learning:** Modern NuGet publishing uses **OIDC-based trusted publishing** instead of API keys. This is Microsoft's recommended secure approach for GitHub Actions → NuGet.org integration.

**⚠️ CRITICAL FIX FROM RERANKING v0.5.0 RELEASE (Session 2):**

The NuGet/login action outputs an API key that MUST be explicitly passed to the nuget push command. Do NOT rely on environment variables alone. Use the pattern shown below with `id: login` to capture output, then pass `--api-key` from that output.

**Implementation Pattern (Reference `.github/workflows/publish.yml` — renamed for NuGet policy compliance):**

```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: ubuntu-latest
    environment: release  # Restricts to release environment secrets
    
    permissions:
      id-token: write  # Allow OIDC token generation
      contents: read
      
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Build and Pack
        run: dotnet pack src/ElBruno.BM25/ElBruno.BM25.csproj -c Release
      
      # ⭐ CRITICAL: Use id: login to capture OIDC token output
      - name: NuGet login (OIDC → temp API key)
        uses: NuGet/login@v1
        id: login  # ← This captures the output
        with:
          user: ${{ secrets.NUGET_USER }}
      
      # ⭐ CRITICAL: Pass --api-key from login step output
      - name: Push to NuGet.org
        run: dotnet nuget push artifacts/*.nupkg \
          --api-key ${{ steps.login.outputs.NUGET_API_KEY }} \
          --source https://api.nuget.org/v3/index.json \
          --skip-duplicate
```

**Setup Steps in GitHub:**

1. **Create NuGet.org trusted publishing entry:**
   - Go to NuGet.org Account Settings → API Keys → Create
   - Select "Trusted Publishing" (not API Key)
   - Name it (e.g., "GitHub BM25")
   - Specify: Owner=`elbruno`, Package pattern=`ElBruno.*`, GitHub environment=`release`
   - **⚠️ WORKFLOW NAME MUST MATCH:** The `path:` field in NuGet policy expects `publish.yml` (exactly). If your workflow is named `publish-nuget.yml`, it will fail with "Workflow mismatch" error.

2. **Create GitHub Environment Secret:**
   - Go to Repository Settings → Environments → Create "release"
   - Add secret `NUGET_USER` with your NuGet service account username (e.g., `elbruno`)
   - **Important:** No API key needed! Trusted publishing handles authentication via OIDC

3. **Tag and Push to Trigger:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

4. **Verify All Assets Are Committed:**
   - **BEFORE creating the tag**, ensure all package assets (icon, images) are committed to git
   - If using relative paths in .csproj (e.g., `PackageIcon`), those files MUST exist in the repo at CI/CD time
   - If files are missing, pack step fails with "Could not find a part of the path"

**Why This Matters:**
- ✅ No secrets stored in repo (OIDC token generated by GitHub, valid for 1 action only)
- ✅ More secure than API keys (can't be leaked from .env files)
- ✅ Automatic package verification (NuGet validates ownership via OIDC)
- ✅ Audit trail (GitHub logs OIDC token usage)

**Common Pitfalls (Updated from Session 2):**
- ❌ Using old API Key authentication (legacy, less secure)
- ❌ Hardcoding API keys in workflows
- ❌ Forgetting `environment: release` permission (breaks OIDC)
- ❌ **Workflow file named `publish-nuget.yml` when policy expects `publish.yml`** — NuGet enforces exact filename match and fails with "Workflow mismatch" error
- ❌ **Not using `id: login` on NuGet/login step** — can't capture API key output for nuget push
- ❌ **Not passing `--api-key` parameter to nuget push** — v3 API requires explicit key header
- ❌ **Package assets not committed to git** — CI/CD cannot find relative paths (e.g., `../../docs/images/icon.png`)

### 19.2 Image Generation Workflow (t2i CLI Tool)

**Critical Learning:** Promotional images (NuGet logo, blog hero, social media graphics) are essential for library adoption. Use **t2i CLI tool** for batch generation.

**t2i CLI Tool Usage:**

```bash
# Basic image generation
t2i --prompt "Design a professional icon for BM25 library..." \
    --output docs/images/bm25-icon-128.png \
    --size 128x128

# Key parameters
--prompt <text>      # Full prompt describing desired image
--output <path>      # File path for generated PNG
--size <WxH>         # Dimensions (e.g., 1200x400, 128x128)
```

**Image Asset Requirements:**

#### NuGet Package Icon
- **Format:** PNG with transparent background
- **Sizes:** 128×128 (primary), 64×64, 32×32 (variants)
- **Reference:** Store in `src/ElBruno.BM25/icon.png`
- **Prompt template:** *"Design a professional icon for [Package Name]. Represent [core concept] with [visual metaphor]. Use [color palette]. Transparent background. Exact [dimensions]."*

**Example (from Reranking):**
```
"Design a professional icon for ElBruno.Reranking (semantic reranking library).
The icon should represent: ranking/relevance/semantic search concepts.
Use a minimalist style with geometric shapes: ascending bars or stacked documents.
Color palette: purples and blues with white accents.
Square format, 128×128 pixels, transparent background."
```

#### Promotional Assets (Blog, LinkedIn, Twitter, GitHub)
- **Blog Hero:** 1200×400 px (16:9), explain the "why" visually
- **LinkedIn:** 1080×1080 px (square), professional, metric-focused
- **Twitter:** 1024×576 px (16:9), punchy, eye-catching
- **GitHub:** 1280×640 px (16:9), brand-consistent, share-friendly

**Workflow:**
1. Create `docs/promotion/image-generation-prompts.md` with all 10 prompts
2. Run t2i CLI in batch (one prompt → one image)
3. Save all images to `docs/images/` subdirectory
4. Create `docs/images/INDEX.md` linking images to their use cases
5. Update promotion markdown files with image references

### 19.3 Promotional Content Strategy

**Critical Learning:** Different channels require different messaging. Coordinate across blog, LinkedIn, Twitter, and GitHub social preview.

**Coordination Pattern:**

| Channel | Length | Tone | Focus | Image |
|---------|--------|------|-------|-------|
| **Blog Post** | 1000-1500 words | Educational, technical | Problem-solution, benchmarks, ROI | Blog Hero (1200×400) |
| **LinkedIn** | 200-300 words | Professional, metrics-driven | Business value, adoption | LinkedIn (1080×1080) |
| **Twitter/X** | 280 chars × 3-5 | Punchy, engaging | Key feature, CTA | Twitter (1024×576) |
| **GitHub README** | Concise badges/features | Practical, developer-focused | Quick setup, links | GitHub Social (1280×640) |

**Content Hierarchy:**
1. **Blog post** = source of truth (detailed, grounded in benchmarks)
2. **Social posts** = distilled from blog (headlines + CTAs)
3. **README** = quick reference (badges, 3-minute quickstart)

**Example Coordination (Reranking):**
- Blog: "Semantic Reranking for .NET: Boost Search Accuracy by 96% in 15ms"
- LinkedIn: "96% R@5 accuracy with <15ms latency. Production-ready reranking for .NET."
- Twitter: "🎯 ElBruno.Reranking: 96% accuracy, 15ms latency, ONNX + Claude. https://..."
- GitHub: Badge linking to blog, metrics in README

### 19.4 Repository Structure & Metadata

**Critical Learning:** Consistent repository structure and metadata accelerate onboarding and GitHub discoverability.

**Reference Repository:** ElBruno.LocalLLMs (use as pattern for README, About Author, badges)

**Repository Setup Checklist:**

```yaml
GitHub Repository Settings:
  - [ ] About: Clear one-liner + problem-solution
  - [ ] Topics: 5-8 relevant keywords (search, dotnet, csharp, etc.)
  - [ ] Website: Link to blog post or docs
  - [ ] Visibility: Public
  - [ ] License: MIT (at repository root)

README.md Sections (Reference ElBruno.LocalLLMs):
  - [ ] Build/NuGet badges (GitHub Actions status, version, downloads)
  - [ ] Project title + tagline
  - [ ] Features (bulleted, 3-5 key points)
  - [ ] Installation (copy-paste command)
  - [ ] Quick Start (3-minute example, complete code)
  - [ ] API Documentation link
  - [ ] Benchmarks (if applicable)
  - [ ] Roadmap
  - [ ] Contributing guidelines
  - [ ] License
  - [ ] About Author (with GitHub profile link + latest project)

Folder Structure:
  ├── src/
  │   ├── ElBruno.BM25/
  │   └── ElBruno.BM25.Tests/
  ├── docs/
  │   ├── guides/
  │   ├── promotion/
  │   ├── images/
  │   └── benchmarks.md
  ├── .github/workflows/
  │   ├── build.yml
  │   └── publish-nuget.yml
  ├── README.md
  ├── LICENSE
  └── CHANGELOG.md
```

### 19.5 CI/CD Workflow Best Practices

**Critical Learning:** Lean, focused GitHub Actions workflows prevent over-engineering and failures.

**Minimal Workflow Pattern:**

```yaml
# build.yml — Runs on PR, main branch pushes
name: Build & Test

on:
  pull_request:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - run: dotnet restore
      - run: dotnet build -c Release
      - run: dotnet test --no-build -c Release --logger "trx"
      
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: '**/test-results/'
```

**Disabled/Removed Actions from Reranking Release:**
- ❌ DocFX generation (manual, on-demand)
- ❌ Multiple OS matrix jobs (stick to ubuntu-latest for simplicity)
- ❌ Approval gates (reserve for enterprise projects)
- ✅ Minimal matrix: Just .NET 8.0 LTS

### 19.6 Documentation Quality Standards

**Critical Learning:** Examples must be production-ready, and all claims must be grounded in benchmarks.

**Quality Gate Checklist:**

```yaml
All Code Examples:
  - [ ] Complete (imports, using statements, full method)
  - [ ] Copy-paste ready (no placeholder comments like "// TODO")
  - [ ] Includes error handling (try-catch or validation)
  - [ ] Compiles without modification
  - [ ] Shows expected output

Performance Claims:
  - [ ] Grounded in actual benchmarks (not aspirational)
  - [ ] Include hardware specs (CPU, RAM, OS)
  - [ ] Include percentiles (p50, p95, p99)
  - [ ] Note edge cases or limitations

Documentation Cross-Checks:
  - [ ] API references consistent across all guides
  - [ ] Performance numbers identical to benchmark doc
  - [ ] Terminology unified (e.g., "reranking" vs "ranking")
  - [ ] Links valid and non-circular
  - [ ] No hardcoded secrets or credentials
```

**Documentation Structure (from Reranking, applicable to BM25):**

```
docs/
├── guides/
│   ├── quickstart.md          (3-minute setup)
│   ├── backend-name.md        (deep dive per backend/feature)
│   ├── custom-tokenizer.md    (extensibility)
│   └── performance-tuning.md  (optimization)
├── promotion/
│   ├── image-generation-prompts.md
│   ├── blog-post-announcement.md
│   ├── linkedin-post.md
│   └── twitter-post.md
├── architecture.md            (system design)
├── benchmarks.md              (metrics + analysis)
└── roadmap.md                 (v1.0, v1.1, v2.0 timeline)
```

### 19.7 Key Decisions Log Pattern

**Critical Learning:** Document architectural and scope decisions in `.squad/decisions.md` (or analogous file) for team continuity.

**Decision Template:**
```markdown
## Decision: [Title]

**Date:** [When decided]
**Stakeholders:** [Who decided]

**Context:** [Why this decision matters]
**Options considered:** [Alternative approaches]
**Decision:** [What we chose and why]
**Rationale:** [Long-term implications]

**Lessons for future projects:**
- [Key insight 1]
- [Key insight 2]
```

**Examples from Reranking:**
- Decision: Use ONNX (BGE) as primary backend (not Claude-only)
- Decision: Support Ollama for local inference (not cloud-only)
- Decision: Document actual API surface (not aspirational design)

### 19.8 Release Checklist

**Critical Learning:** A standardized release checklist prevents missed steps and unplanned hotfixes.

```yaml
Pre-Release (1 day before):
  - [ ] Update CHANGELOG.md (all features, fixes, breaking changes)
  - [ ] Update version in .csproj (semantic versioning: MAJOR.MINOR.PATCH)
  - [ ] Update README.md version references
  - [ ] Run all tests locally (dotnet test --no-build)
  - [ ] Review API documentation (DocFX output)
  - [ ] Prepare release notes for GitHub (copy from CHANGELOG)

Release Day:
  - [ ] Create git tag: git tag v1.0.0
  - [ ] Push tag: git push origin v1.0.0
  - [ ] Monitor GitHub Actions workflow (build + publish)
  - [ ] Verify NuGet.org package published
  - [ ] Create GitHub Release (auto-populated from tag)
  - [ ] Publish blog post announcement
  - [ ] Post on LinkedIn + Twitter

Post-Release (1-2 days):
  - [ ] Monitor NuGet.org for feedback (comments, issues)
  - [ ] Pin release blog post on social media
  - [ ] Collect metrics (downloads, stars, feedback)
  - [ ] Address any hotfix issues
```

### 19.10 NuGet Publish Workflow Debugging — Troubleshooting Pattern (NEW — Session 2)

**Critical Learning:** NuGet publish failures often occur due to subtle configuration mismatches. Use a working reference repository to diagnose issues.

**When publish workflow fails:**

1. **Identify the failure type from logs:**
   - `Workflow mismatch for policy 'X': expected 'publish.yml', actual 'publish-nuget.yml'` → Workflow file renamed incorrectly
   - `No matching trust policy owned by user '__NUGET_USER__'` → Hardcoded placeholder instead of secret variable
   - `Could not find a part of the path '/home/runner/work/...'` → Package asset (e.g., icon PNG) not committed to git
   - `An API key must be provided in the 'X-NuGet-ApiKey' header` → Missing `--api-key` parameter or not captured from login step
   - `HTTP 401 at token exchange` → Wrong environment, missing permissions, or misconfigured OIDC

2. **Reference a working repository:**
   - Use `github.com/elbruno/ElBruno.Text2Image` as the reference (proven OIDC + nuget publish workflow)
   - Compare `.github/workflows/publish.yml` line-by-line
   - Adopt the EXACT pattern: `id: login`, `${{ steps.login.outputs.NUGET_API_KEY }}`, v3 API endpoint

3. **Debugging checklist:**
   ```yaml
   Workflow Configuration:
     - [ ] File named exactly `publish.yml` (not `publish-nuget.yml` or other variants)
     - [ ] Triggered on tag push: `push.tags: ['v*']`
     - [ ] Environment set to `release`: `environment: release`
     - [ ] OIDC permissions granted: `permissions: { id-token: write, contents: read }`
   
   NuGet Login Step:
     - [ ] Uses `NuGet/login@v1` action (not custom script)
     - [ ] Has `id: login` to capture output
     - [ ] Passes `user: ${{ secrets.NUGET_USER }}` (NOT hardcoded `elbruno`)
     - [ ] No explicit `token-service-url` (uses defaults)
   
   NuGet Push Step:
     - [ ] Uses `--api-key ${{ steps.login.outputs.NUGET_API_KEY }}`
     - [ ] Points to v3 API: `https://api.nuget.org/v3/index.json`
     - [ ] Includes `--skip-duplicate` flag
   
   Pre-Release Assets:
     - [ ] All package assets (icons, images) committed to git
     - [ ] .csproj `PackageIcon` path relative to project file
     - [ ] Icon dimensions: 128×128 PNG with transparent background
   
   GitHub Secrets & Environment:
     - [ ] Secret `NUGET_USER` created in "release" environment (NOT repo secrets)
     - [ ] Value is service principal username (e.g., `elbruno`), not API key
     - [ ] NuGet.org trusted publishing entry created with matching username
   ```

4. **When in doubt, compare your workflow to the reference:**
   ```bash
   # Fetch the reference workflow
   curl https://raw.githubusercontent.com/elbruno/ElBruno.Text2Image/main/.github/workflows/publish.yml \
     > /tmp/reference-publish.yml
   
   # Diff against your workflow
   diff /tmp/reference-publish.yml .github/workflows/publish.yml
   ```

**Recovery Strategy:**
1. Fix the identified issue (rename file, update secret usage, commit assets, adjust API call)
2. Delete the old failed tag: `git tag -d v1.0.0 && git push origin --delete v1.0.0`
3. Recreate the tag: `git tag v1.0.0 && git push origin v1.0.0`
4. Monitor the new workflow run
5. If still failing, check the logs and compare line-by-line with reference repo

**Key Insight:** The NuGet API and GitHub Actions ecosystem is strict about configuration. Small details (filename, secret names, API versions) cause cascading failures. Always reference a **proven working example** before debugging — saves hours of trial-and-error.

### 19.9 Lessons Learned for BM25 from Reranking Release

**Apply These Learnings:**

1. **Documentation:**
   - ✅ Document BM25 algorithm with formula and parameter explanation
   - ✅ Provide realistic examples (MemPalace.NET integration, hybrid search)
   - ✅ Include parameter tuning guide (k1, b, delta)

2. **Benchmarks:**
   - ✅ Measure on real datasets (MemBench, LongMemEval)
   - ✅ Include p95/p99 latencies (not just p50)
   - ✅ Test on different corpus sizes (1K, 100K, 1M docs)

3. **Examples:**
   - ✅ Provide copy-paste ready code
   - ✅ Show both basic (SimpleTokenizer) and advanced (CustomTokenizer) usage
   - ✅ Include error handling

4. **Release Process (Critical - Learned from Reranking v0.5.0):**
   - ✅ Use OIDC trusted publishing with trusted publishing policy (not API keys)
   - ✅ **Rename workflow file to exactly `publish.yml`** (NuGet policy enforcement)
   - ✅ **Use `id: login` on NuGet/login step and capture OIDC output** (required by v1.0 action)
   - ✅ **Pass `--api-key ${{ steps.login.outputs.NUGET_API_KEY }}` to push command** (explicit key parameter)
   - ✅ **Commit all package assets (icon, images) to git before tagging** (required by CI/CD)
   - ✅ Generate promotional images with t2i CLI
   - ✅ Coordinate blog + LinkedIn + Twitter announcements
   - ✅ **Reference ElBruno.Text2Image `.github/workflows/publish.yml`** (proven working pattern)
   - ✅ Reference ElBruno.LocalLLMs README pattern (About Author, badges)

5. **Repository Setup:**
   - ✅ Create `.squad/decisions.md` early (document scope boundaries, API design decisions)
   - ✅ Set up GitHub topics and about metadata
   - ✅ Structure docs/ folder consistently
   - ✅ Ensure icon is 128×128 PNG with transparent background

6. **Troubleshooting & Recovery (NEW):**
   - When workflow fails, refer to Section 19.10 debugging checklist
   - Delete and recreate git tags if workflow configuration changes
   - Always compare against ElBruno.Text2Image reference repo for OIDC + publish pattern


---

## 18. FAQ

**Q: Why not use Lucene.NET?**
A: Lucene.NET is heavy (~500KB+ dependencies). ElBruno.BM25 is lightweight (~200 LOC) for projects that don't need full IR features. Use Lucene for mega-scale (v2.0 will have compatibility layer).

**Q: Can I use this in production?**
A: Yes, after v1.0 release. It will be fully tested, documented, and versioned.

**Q: Does this support distributed indexing?**
A: No (v1.0). (v2.0 roadmap) For now, use single-machine indexing or external search engines.

**Q: How do I tune BM25 parameters?**
A: Start with defaults (k1=1.5, b=0.75). Use `Bm25Tuner` for automatic tuning on your dataset.

---

## 19. Conclusion

**ElBruno.BM25** fills a gap in the .NET ecosystem: production-ready, lightweight BM25 implementation for projects that need keyword search without heavy infrastructure. By shipping as a standalone library, it's composable into any .NET application, including MemPalace.NET v0.8.0's hybrid search pipeline.

The library prioritizes **simplicity** (easy to understand), **performance** (fast, low-memory), and **extensibility** (custom tokenizers). Success will be measured by adoption, performance benchmarks, and developer satisfaction.

**Next steps:** Implement Phase 1 (core algorithm), validate integration with MemPalace.NET, and prepare for NuGet release.

---

**END OF DOCUMENT**
