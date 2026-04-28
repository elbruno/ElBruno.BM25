namespace ElBruno.BM25;

using System.Collections.Generic;

/// <summary>
/// Represents a search result from a BM25 query.
/// </summary>
/// <typeparam name="T">The type of document being searched.</typeparam>
public class SearchResult<T>
{
    /// <summary>
    /// Gets or sets the matched document.
    /// </summary>
    public required T Document { get; set; }

    /// <summary>
    /// Gets or sets the BM25 score for this result (unbounded, typically 0-20 for short queries).
    /// Higher scores indicate stronger relevance.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the rank (position) of this result in the result set (1-based).
    /// </summary>
    public double Rank { get; set; }

    /// <summary>
    /// Gets or sets the list of query terms that matched in this document.
    /// </summary>
    public List<string> MatchedTerms { get; set; } = new();

    /// <summary>
    /// Gets or sets the per-term score breakdown showing contribution of each query term.
    /// Key: term, Value: term's contribution to overall score.
    /// </summary>
    public Dictionary<string, double> TermScores { get; set; } = new();
}
