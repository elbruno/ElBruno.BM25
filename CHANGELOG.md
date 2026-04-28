# Changelog

All notable changes to ElBruno.BM25 will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

---

## [0.5.0] - 2026-04-28

### Added

#### Core BM25 Implementation
- `Bm25Index<T>` — Main class for full-text search indexing and retrieval
- Proven BM25 ranking algorithm with configurable parameters (k1, b, delta)
- Support for any document type via generic `<T>` and content selector function
- Fast search with O(Q × avg_df) complexity

#### Tokenization System
- `ITokenizer` interface for pluggable tokenization strategies
- `SimpleTokenizer` — Whitespace-based tokenization with lowercase normalization
- `EnglishTokenizer` — English-specific tokenizer with Porter stemming
- `CustomTokenizer` — User-defined tokenization logic
- Extensible for domain-specific implementations

#### Indexing Features
- Dynamic document management (add, remove, reindex)
- Per-document token counting and term frequency tracking
- Inverted index with efficient posting list storage
- IDF caching for performance optimization
- Zero external dependencies (pure C# implementation)

#### Search Operations
- Single-query search with topK and threshold filtering
- Batch search for multiple queries with async support
- Cancellation token support for long-running operations
- Term-level score breakdown (MatchedTerms, TermScores)

#### Advanced Features
- `Bm25Tuner<T>` — Automatic parameter optimization via grid search
- Score explanation with `ExplainScore()` and `ExplainScoreDetailed()`
- Index statistics (document count, term count, vocabulary richness)
- Term lookup (`GetTerms()`, `GetTermDocuments()`)

#### Persistence
- `SaveIndex()` — Serialize index to JSON format
- `LoadIndex()` — Deserialize index from JSON
- Index versioning support
- Portable format across .NET versions

#### Preset Parameter Profiles
- `Bm25Parameters.Default` — Balanced (k1=1.5, b=0.75, delta=0.5)
- `Bm25Parameters.Aggressive` — For large corpora (k1=2.0, b=1.0, delta=0.5)
- `Bm25Parameters.Conservative` — For small corpora (k1=1.0, b=0.5, delta=0.5)

### Features

#### Performance
- Index 1M documents in <5 seconds
- Search latency <50ms on 1M document index
- Memory footprint <1GB for 1M documents
- Batch search optimization for multiple queries
- IDF caching reduces repeated scoring overhead

#### Reliability
- 70+ comprehensive unit tests
- Edge case coverage (empty queries, single document, no matches)
- Error handling with descriptive exceptions
- Null parameter validation on all public methods

#### Extensibility
- Custom tokenizer support for domain-specific needs
- Parameter tuning with multiple metrics (Precision, Recall, F1, NDCG)
- Score explanation for debugging and transparency

#### Documentation
- Complete API reference with examples
- Getting started guide (5-minute walkthrough)
- Advanced usage guide (tuning, persistence, integration)
- Architecture documentation with algorithm details
- Contributing guidelines

### Technical Details

#### BM25 Algorithm
- Full BM25 formula implementation with:
  - Term frequency (TF) normalization with saturation (k1 parameter)
  - Inverse document frequency (IDF) with smoothing
  - Document length normalization (b parameter)
- Handles edge cases:
  - Zero average document length
  - Extremely long or short documents
  - Rare terms with zero document frequency

#### Implementation Characteristics
- Zero external dependencies (no NuGet packages required)
- .NET 8.0+ support with modern async/await patterns
- Generics-based design for type safety
- Thread-safe for concurrent reads
- XML documentation comments on all public APIs

#### Data Structures
- `Dictionary<string, Dictionary<T, int>>` for inverted index
- Document length cache (`Dictionary<T, int>`)
- IDF cache (`Dictionary<string, double>`)
- Posting list optimization for common terms

### Testing

- **Unit Tests**: 70+ covering all functionality
- **Integration Tests**: Search pipeline and persistence
- **Performance Tests**: Benchmarks for indexing and search
- **Edge Case Tests**: Empty inputs, null values, boundary conditions
- **Tokenizer Tests**: All three tokenizer implementations

### Documentation

- `README.md` — Overview, quick start, examples, use cases
- `GETTING_STARTED.md` — Step-by-step guide for beginners
- `API_REFERENCE.md` — Complete API documentation
- `ADVANCED_USAGE.md` — Tuning, persistence, integration patterns
- `ARCHITECTURE.md` — Algorithm details, design decisions, trade-offs
- `CONTRIBUTING.md` — Development guidelines, testing, code style

### Project Metadata

- **License**: MIT
- **Repository**: https://github.com/ElBruno/ElBruno.BM25
- **NuGet Package**: ElBruno.BM25
- **Target Framework**: .NET 8.0+
- **Language Version**: C# 12

### Known Limitations

- **No semantic search** — BM25 is lexical-only; use vector embeddings for semantic matching
- **No multi-field indexing** — All content flattened to single field
- **Bag-of-words model** — Word order ignored
- **English tokenizer only** — Language-specific stemming limited to English
- **In-memory only** — All data stored in RAM, not suitable for extremely large corpora
- **No phrase search** — Exact phrase matching not supported

### Performance Characteristics

| Operation | Dataset | Time |
|-----------|---------|------|
| Index | 1M documents | <5 seconds |
| Search | 1M documents, single query | <50ms |
| Batch Search | 1M documents, 100 queries | <5 seconds |
| Save to Disk | 1M documents | <1 second |
| Load from Disk | 1M documents | <2 seconds |

### Use Cases

✅ RAG systems (Retrieval-Augmented Generation)  
✅ Knowledge base search  
✅ Hybrid search (combined with vector search)  
✅ Full-text search without Elasticsearch  
✅ Information retrieval education  
✅ Embedded search for .NET applications  

### Future Roadmap

Not included in v0.5.0 but planned:

- [ ] BM25F (multi-field variant)
- [ ] Phrase query support
- [ ] Stop word filtering
- [ ] Query expansion/synonyms
- [ ] Relevance feedback
- [ ] Distributed indexing
- [ ] Performance optimizations
- [ ] Additional language tokenizers

---

## Notes for v0.5.0

**Stability**: This is the first public release. API is considered stable, though bug fixes and performance improvements may be released in patch versions.

**Migration**: No migration needed (first release).

**Compatibility**: Fully compatible with .NET 8.0+. No breaking changes planned for v0.x releases.

**Support**: Report issues on GitHub. Check existing issues and discussions before posting.

---

## Future Versions

### v0.5.1 (Planned)
- Bug fixes based on community feedback
- Performance improvements
- Additional test coverage

### v0.6.0 (Planned)
- BM25F support (multi-field ranking)
- Stop word filtering
- Additional tokenizers

### v1.0.0 (Planned)
- API stability guarantee
- Comprehensive performance tuning
- Extended documentation

---

## How to Contribute

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on:
- Building and testing locally
- Code style requirements
- Submitting pull requests
- Reporting issues

---

**Start using ElBruno.BM25 today! Install via NuGet:**

```bash
dotnet add package ElBruno.BM25
```

For more information, see the [README](README.md) or [Getting Started Guide](docs/GETTING_STARTED.md).
