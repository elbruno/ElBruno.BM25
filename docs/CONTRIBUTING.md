# Contributing to ElBruno.BM25

Thank you for your interest in contributing! This guide explains how to build, test, and contribute code.

## Local Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- A text editor or IDE (Visual Studio, VS Code, Rider)
- Git

### Clone and Build

```bash
git clone https://github.com/ElBruno/ElBruno.BM25.git
cd ElBruno.BM25

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run all tests
dotnet test
```

## Project Structure

```
ElBruno.BM25/
├── src/
│   └── ElBruno.BM25/
│       ├── Bm25Index.cs              # Main indexing class
│       ├── Bm25Parameters.cs          # Algorithm parameters
│       ├── Bm25Tuner.cs              # Parameter optimization
│       ├── ITokenizer.cs             # Tokenizer interface
│       ├── ScoreExplanation.cs        # Score breakdown
│       ├── SearchResult.cs            # Result wrapper
│       ├── TuningMetric.cs            # Tuning metrics enum
│       ├── Tokenizers/
│       │   ├── SimpleTokenizer.cs
│       │   ├── EnglishTokenizer.cs
│       │   └── CustomTokenizer.cs
│       └── ElBruno.BM25.csproj
├── tests/
│   └── ElBruno.BM25.Tests/
│       ├── Bm25IndexTests.cs         # Core functionality tests
│       ├── EdgeCaseTests.cs          # Edge cases
│       ├── PerformanceTests.cs       # Benchmarks
│       ├── PersistenceTests.cs       # Save/load tests
│       ├── Phase3AdvancedFeaturesTests.cs
│       ├── TokenizerTests.cs         # Tokenizer tests
│       ├── Data/
│       │   └── TestDocuments.cs      # Test data
│       └── ElBruno.BM25.Tests.csproj
├── docs/                              # Documentation
├── README.md
└── CONTRIBUTING.md
```

## Code Style Guidelines

### Naming Conventions

```csharp
// Classes and methods: PascalCase
public class Bm25Index<T> { }
public void AddDocument(T document) { }

// Private fields: camelCase with underscore prefix
private Dictionary<string, int> _termFrequencies;
private ITokenizer _tokenizer;

// Constants: PascalCase
public const double DefaultK1 = 1.5;

// Local variables: camelCase
var documentCount = index.DocumentCount;
```

### Formatting

```csharp
// 1. Keep lines under 120 characters
// 2. Use 4 spaces for indentation (no tabs)
// 3. Braces on new lines for class/method definitions
public class MyClass
{
    public void MyMethod()
    {
        // code
    }
}

// 4. Single-line statements in if/for without braces only for simple cases
if (x > 0) count++;

// 5. Otherwise use braces
if (x > 0)
{
    DoSomething();
    DoSomethingElse();
}
```

### Documentation

All public APIs require XML doc comments:

```csharp
/// <summary>
/// Searches the index for documents matching the query.
/// </summary>
/// <param name="query">The search query string.</param>
/// <param name="topK">Maximum number of results to return (default: 10).</param>
/// <returns>A list of (document, score) tuples sorted by score descending.</returns>
/// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
public List<(T document, double score)> Search(
    string query,
    int topK = 10
)
{
    // implementation
}
```

### No Unnecessary Comments

```csharp
// ❌ BAD - obvious comment
i++;  // Increment i

// ✅ GOOD - explains WHY not WHAT
docLengths[doc] = tokens.Count;  // Cache document length for faster BM25 calculation

// ✅ GOOD - complex logic needs comment
var denominator = tf + k1 * (1 - b + b * (docLen / Math.Max(_parameters.AvgDocLength, 1)));
// BM25 length normalization factor
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=Bm25IndexTests"

# Run with verbose output
dotnet test --verbosity:detailed

# Run with coverage (if installed)
dotnet test /p:CollectCoverage=true
```

### Writing Tests

Use xUnit. Follow AAA pattern (Arrange, Act, Assert):

```csharp
[Fact]
public void TestSearchBasic_ExactMatch()
{
    // Arrange
    var docs = new List<TestDoc> { new() { Id = 1, Content = "machine learning" } };
    var index = new Bm25Index<TestDoc>(docs, d => d.Content);

    // Act
    var results = index.Search("machine");

    // Assert
    Assert.NotEmpty(results);
    Assert.Equal(1, results.First().document.Id);
}
```

**Test naming:** `[MethodName]_[Scenario]_[ExpectedBehavior]`

### Test Coverage Goals

- Unit tests for all public methods
- Edge cases: empty queries, no results, single document
- Error cases: null parameters, invalid operations
- Performance: benchmark critical operations

## Adding Features

### New Tokenizer

1. Implement `ITokenizer`:

```csharp
using ElBruno.BM25;

public class MyTokenizer : ITokenizer
{
    public string Name => "MyTokenizer";

    public List<string> Tokenize(string text)
    {
        // Your tokenization logic
        return new List<string>();
    }

    public string Normalize(string term)
    {
        // Your normalization logic
        return term;
    }
}
```

2. Add unit tests in `TokenizerTests.cs`
3. Document in ADVANCED_USAGE.md
4. Add to list of tokenizers in README

### New Public Method

1. Implement method in appropriate class
2. Add XML doc comments
3. Add unit tests covering:
   - Normal use case
   - Edge cases (empty input, null, etc.)
   - Error cases
4. Update API_REFERENCE.md
5. Update CHANGELOG.md

### Algorithm Changes

1. Verify against test suite first
2. Add tests for new behavior
3. Update ARCHITECTURE.md if design changes
4. Benchmark performance impact
5. Document in CHANGELOG.md

## Code Review Checklist

Before submitting PR:

- [ ] Code builds without warnings: `dotnet build`
- [ ] All tests pass: `dotnet test`
- [ ] Code follows style guide (no obvious formatting issues)
- [ ] New public methods have XML doc comments
- [ ] New features have unit tests
- [ ] No breaking changes to public API (or documented)
- [ ] CHANGELOG.md updated
- [ ] Documentation updated if needed
- [ ] Performance impact considered (no regressions)

## Submitting Changes

### Fork and Branch

```bash
# Fork on GitHub, then:
git clone https://github.com/YOUR_USERNAME/ElBruno.BM25.git
cd ElBruno.BM25
git checkout -b feature/my-feature-name
```

### Commit Messages

Use clear, descriptive commit messages:

```bash
# ✅ Good
git commit -m "Add support for custom stop words in SimpleTokenizer"
git commit -m "Fix BM25 parameter calculation for edge case with avgdl=0"

# ❌ Bad
git commit -m "fix stuff"
git commit -m "WIP"
git commit -m "asdf"
```

Format:
```
[Type] Brief description (50 chars max)

Longer explanation if needed (72 char line limit).
Explain what changed and why.

- Bullet points for multiple changes
- Each change on separate line
```

Types: Feature, Fix, Docs, Refactor, Test, Perf

### Push and Create PR

```bash
git push origin feature/my-feature-name
# Go to GitHub and create Pull Request
```

**PR Title:** `[Feature] Add custom tokenizer support`

**PR Description:**
```markdown
## Description
Brief overview of changes

## Motivation
Why is this change needed?

## Changes
- Item 1
- Item 2

## Testing
How to verify the changes work

## Checklist
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
```

## Reporting Issues

When reporting bugs:

1. **Title:** Clear, specific description
   - ✅ "Search returns no results for queries with stopwords"
   - ❌ "doesn't work"

2. **Environment:**
   ```
   - OS: Windows 10
   - .NET Version: 8.0.1
   - ElBruno.BM25 Version: 0.5.0
   ```

3. **Reproduction:**
   ```csharp
   // Minimal code to reproduce
   var index = new Bm25Index<Article>(...);
   var results = index.Search("query");
   // Expected: 5 results, Actual: 0 results
   ```

4. **Attachments:**
   - Stack trace (if applicable)
   - Test data (if safe to share)

## Performance Considerations

### Optimization Areas

1. **Search Performance**
   - Minimize memory allocations
   - Reuse collections where possible
   - Cache IDF values

2. **Indexing Performance**
   - Batch operations when possible
   - Lazy evaluation where applicable

3. **Memory Usage**
   - Consider dictionary overhead
   - Profile large indexes (1M+ docs)

### Benchmarking

Add performance tests using xUnit:

```csharp
[Fact]
public void Benchmark_Search_1MDocuments()
{
    var largeIndex = CreateLargeIndex(1_000_000);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    var results = largeIndex.Search("test", topK: 10);
    
    sw.Stop();
    Assert.True(sw.ElapsedMilliseconds < 100, $"Search took {sw.ElapsedMilliseconds}ms");
}
```

Run before and after optimization:
```bash
dotnet test PerformanceTests.cs --verbosity:detailed
```

## Documentation

When you add features:

1. **Update API_REFERENCE.md** with method signatures and examples
2. **Update GETTING_STARTED.md** if it affects beginner workflow
3. **Update ADVANCED_USAGE.md** for advanced scenarios
4. **Update README.md** if feature is major
5. **Update CHANGELOG.md** with version and description

## Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR.MINOR.PATCH** (e.g., 0.5.0)
- **MAJOR:** Breaking API changes
- **MINOR:** New features (backward compatible)
- **PATCH:** Bug fixes

## Release Process

1. Update version in `ElBruno.BM25.csproj`
2. Update `CHANGELOG.md` with new version
3. Commit: `Release version X.Y.Z`
4. Create Git tag: `git tag vX.Y.Z`
5. Push: `git push && git push --tags`
6. NuGet package automatically builds (if CI/CD configured)

## Questions?

- 📖 See [README](../README.md) for overview
- 📚 See [ARCHITECTURE.md](./docs/ARCHITECTURE.md) for design details
- 🐛 Check existing issues for similar problems
- 💬 Start a discussion for design questions

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to ElBruno.BM25! 🎉**
