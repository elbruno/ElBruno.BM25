namespace ElBruno.BM25.Tokenizers;

using System;
using System.Collections.Generic;
using ElBruno.BM25;

/// <summary>
/// Custom tokenizer that allows user-defined tokenization logic.
/// </summary>
public class CustomTokenizer : ITokenizer
{
    private readonly Func<string, List<string>> _tokenizeFn;

    /// <summary>
    /// Gets the name of this tokenizer.
    /// </summary>
    public string Name => "Custom";

    /// <summary>
    /// Initializes a new instance of the CustomTokenizer class.
    /// </summary>
    /// <param name="tokenizeFn">A function that tokenizes text into a list of terms.</param>
    /// <exception cref="ArgumentNullException">Thrown when tokenizeFn is null.</exception>
    public CustomTokenizer(Func<string, List<string>> tokenizeFn)
    {
        _tokenizeFn = tokenizeFn ?? throw new ArgumentNullException(nameof(tokenizeFn));
    }

    /// <summary>
    /// Tokenizes text using the custom tokenization function.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <returns>A list of terms from the custom tokenizer.</returns>
    public List<string> Tokenize(string text)
    {
        return _tokenizeFn(text);
    }

    /// <summary>
    /// Normalizes a term (delegates to custom function for normalization).
    /// Default: returns term as-is. Override by providing custom normalization in tokenizer function.
    /// </summary>
    /// <param name="term">The term to normalize.</param>
    /// <returns>The normalized term.</returns>
    public string Normalize(string term)
    {
        return term ?? string.Empty;
    }
}
