using System;
using System.Collections.Generic;
using System.Linq;

namespace ElBruno.BM25.Tests.Data;

/// <summary>
/// Centralized test data for comprehensive test coverage.
/// Provides consistent, reusable document sets for unit and performance tests.
/// </summary>
public static class TestDocuments
{
    /// <summary>
    /// Simple test data: 3 documents with predictable term frequencies.
    /// Used for basic relevance, ranking, and scoring tests.
    /// </summary>
    public static class Simple
    {
        public static readonly TestDoc[] Documents = new[]
        {
            new TestDoc 
            { 
                Id = 1, 
                Title = "Document 1",
                Content = "query query query" // High frequency of 'query' (3x)
            },
            new TestDoc 
            { 
                Id = 2, 
                Title = "Document 2",
                Content = "query query" // Medium frequency of 'query' (2x)
            },
            new TestDoc 
            { 
                Id = 3, 
                Title = "Document 3",
                Content = "query" // Low frequency of 'query' (1x)
            }
        };

        // Expected: When searching for "query", Doc1 > Doc2 > Doc3 by score
    }

    /// <summary>
    /// Length normalization test data: long vs short documents with same term frequency.
    /// Used to verify document length normalization (B parameter) is working.
    /// </summary>
    public static class WithLength
    {
        public static readonly TestDoc[] Documents = new[]
        {
            new TestDoc 
            { 
                Id = 1, 
                Title = "Short Document",
                Content = "signal " + string.Join(" ", Enumerable.Repeat("filler", 10))
                // Short doc: 1 "signal" + 10 fillers = 11 words
            },
            new TestDoc 
            { 
                Id = 2, 
                Title = "Long Document",
                Content = "signal " + string.Join(" ", Enumerable.Repeat("filler", 1000))
                // Long doc: 1 "signal" + 1000 fillers = 1001 words
            }
        };

        // Expected: Short doc should score higher (length normalization)
    }

    /// <summary>
    /// Unicode and multilingual test data.
    /// Used for edge case tests with non-ASCII characters.
    /// </summary>
    public static class WithUnicode
    {
        public static readonly TestDoc[] Documents = new[]
        {
            new TestDoc 
            { 
                Id = 1, 
                Title = "Emoji Test",
                Content = "Hello world 😀 🚀 ✨" 
            },
            new TestDoc 
            { 
                Id = 2, 
                Title = "Chinese Test",
                Content = "中文文本测试 这是一个例子"
            },
            new TestDoc 
            { 
                Id = 3, 
                Title = "Arabic Test",
                Content = "النص العربي الاختبار مثال"
            },
            new TestDoc 
            { 
                Id = 4, 
                Title = "Mixed Scripts",
                Content = "Hello мир 世界 🌍 test тест 测试"
            }
        };

        // Expected: Tokenizer handles Unicode appropriately
    }

    /// <summary>
    /// Special characters and punctuation test data.
    /// Used for edge cases with symbols, numbers, and formatting.
    /// </summary>
    public static class WithSpecialCharacters
    {
        public static readonly TestDoc[] Documents = new[]
        {
            new TestDoc 
            { 
                Id = 1, 
                Title = "Email and URLs",
                Content = "Contact: john@example.com or visit https://example.com"
            },
            new TestDoc 
            { 
                Id = 2, 
                Title = "Numbers and Units",
                Content = "The price is $99.99 and weight is 5kg at 30°C"
            },
            new TestDoc 
            { 
                Id = 3, 
                Title = "Punctuation Heavy",
                Content = "What? Really!!! Yes; no, maybe... (definitely) [possibly] {uncertain}"
            },
            new TestDoc 
            { 
                Id = 4, 
                Title = "Programming Symbols",
                Content = "const x = {key: \"value\"}; if (x) { console.log(x); }"
            }
        };

        // Expected: Special chars handled per tokenization rules
    }

    /// <summary>
    /// Large corpus: 1000 documents with random/realistic content.
    /// Used for performance and scaling tests.
    /// </summary>
    public static class LargeCorpus
    {
        /// <summary>
        /// Generates a large corpus of random documents.
        /// Each document has realistic content with common words and varying lengths.
        /// </summary>
        public static TestDoc[] GenerateDocuments(int count = 1000)
        {
            var random = new Random(42); // Fixed seed for reproducibility
            var documents = new TestDoc[count];
            var commonWords = new[]
            {
                "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
                "hello", "world", "system", "search", "index", "document", "query",
                "algorithm", "performance", "optimization", "testing", "quality"
            };

            for (int i = 0; i < count; i++)
            {
                var wordCount = random.Next(20, 200);
                var words = new List<string>();

                for (int w = 0; w < wordCount; w++)
                {
                    words.Add(commonWords[random.Next(commonWords.Length)]);
                }

                documents[i] = new TestDoc
                {
                    Id = i + 1,
                    Title = $"Document {i + 1}",
                    Content = string.Join(" ", words)
                };
            }

            return documents;
        }

        // Expected: Used for performance benchmarks (indexing, search speed)
    }

    /// <summary>
    /// Test data for edge cases: empty, null, very long, very short content.
    /// </summary>
    public static class EdgeCases
    {
        public static readonly TestDoc[] Documents = new[]
        {
            new TestDoc 
            { 
                Id = 1, 
                Title = "Empty Content",
                Content = ""
            },
            new TestDoc 
            { 
                Id = 2, 
                Title = "Whitespace Only",
                Content = "   \t\n   "
            },
            new TestDoc 
            { 
                Id = 3, 
                Title = "Single Word",
                Content = "word"
            },
            new TestDoc 
            { 
                Id = 4, 
                Title = "Very Long Document",
                Content = string.Join(" ", Enumerable.Range(1, 10000).Select(i => $"word{i}"))
            }
        };

        // Expected: Edge cases handled gracefully
    }

    /// <summary>
    /// Test data for case sensitivity validation.
    /// </summary>
    public static class CaseSensitivity
    {
        public static readonly TestDoc[] Documents = new[]
        {
            new TestDoc 
            { 
                Id = 1, 
                Title = "Lowercase",
                Content = "hello world test"
            },
            new TestDoc 
            { 
                Id = 2, 
                Title = "Uppercase",
                Content = "HELLO WORLD TEST"
            },
            new TestDoc 
            { 
                Id = 3, 
                Title = "Mixed Case",
                Content = "Hello World Test"
            }
        };

        // Expected: Case handling depends on tokenizer configuration
    }
}

/// <summary>
/// Simple test document structure for test data.
/// </summary>
public class TestDoc
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public override string ToString() => $"TestDoc(Id={Id}, Title={Title}, Length={Content.Length})";
}
