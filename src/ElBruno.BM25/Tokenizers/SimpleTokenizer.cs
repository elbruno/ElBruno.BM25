namespace ElBruno.BM25.Tokenizers;

using System;
using System.Collections.Generic;
using ElBruno.BM25;

/// <summary>
/// Simple whitespace-based tokenizer with lowercase normalization.
/// No stemming or advanced processing.
/// </summary>
public class SimpleTokenizer : ITokenizer
{
    /// <summary>
    /// Gets the name of this tokenizer.
    /// </summary>
    public string Name => "Simple";

    /// <summary>
    /// Tokenizes text by splitting on whitespace and removing punctuation.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <returns>A list of lowercase terms.</returns>
    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();
        
        var terms = new List<string>();
        var chars = text.ToLowerInvariant().ToCharArray();
        var currentTerm = new System.Text.StringBuilder();

        foreach (var c in chars)
        {
            if (char.IsLetterOrDigit(c))
            {
                currentTerm.Append(c);
            }
            else if (currentTerm.Length > 0)
            {
                terms.Add(currentTerm.ToString());
                currentTerm.Clear();
            }
        }

        if (currentTerm.Length > 0)
            terms.Add(currentTerm.ToString());

        return terms;
    }

    /// <summary>
    /// Normalizes a term to lowercase.
    /// </summary>
    /// <param name="term">The term to normalize.</param>
    /// <returns>The lowercased term.</returns>
    public string Normalize(string term)
    {
        return term?.ToLowerInvariant() ?? string.Empty;
    }
}
