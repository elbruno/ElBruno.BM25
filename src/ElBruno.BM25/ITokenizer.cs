namespace ElBruno.BM25;

using System.Collections.Generic;

/// <summary>
/// Pluggable tokenization strategy for BM25 indexing and search.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Tokenizes text into a list of terms.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <returns>A list of normalized terms.</returns>
    List<string> Tokenize(string text);

    /// <summary>
    /// Normalizes a single term (e.g., lowercase, stemming).
    /// </summary>
    /// <param name="term">The term to normalize.</param>
    /// <returns>The normalized term.</returns>
    string Normalize(string term);

    /// <summary>
    /// Gets the name or variant of this tokenizer (e.g., "English", "Simple").
    /// </summary>
    string Name { get; }
}
