namespace ElBruno.BM25;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides detailed breakdown of why a document scored a certain way for a BM25 query.
/// Useful for debugging and understanding relevance scoring.
/// </summary>
public class ScoreExplanation
{
    /// <summary>
    /// Gets or sets the total BM25 score for the document and query.
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of IDF (Inverse Document Frequency) values per query term.
    /// IDF measures how rare a term is across the corpus.
    /// </summary>
    public Dictionary<string, double> TermIDFs { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary of TF (Term Frequency) values per query term in the document.
    /// TF measures how often a term appears in the document.
    /// </summary>
    public Dictionary<string, double> TermFrequencies { get; set; } = new();

    /// <summary>
    /// Gets or sets the length normalization factor applied to the document.
    /// Compensates for documents of varying lengths.
    /// </summary>
    public double LengthNormalization { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of individual BM25 scores per query term.
    /// The sum of these scores equals TotalScore.
    /// </summary>
    public Dictionary<string, double> TermScores { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of unique query terms found in the document.
    /// </summary>
    public int MatchedTermCount { get; set; }

    /// <summary>
    /// Gets or sets the document length (token count).
    /// </summary>
    public int DocumentLength { get; set; }

    /// <summary>
    /// Gets or sets the average document length in the corpus.
    /// </summary>
    public double AverageDocumentLength { get; set; }

    /// <summary>
    /// Gets or sets the K1 parameter used in BM25 calculation.
    /// </summary>
    public double K1Parameter { get; set; }

    /// <summary>
    /// Gets or sets the B parameter used in BM25 calculation.
    /// </summary>
    public double BParameter { get; set; }
}
