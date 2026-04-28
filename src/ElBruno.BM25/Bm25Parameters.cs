namespace ElBruno.BM25;

using System;
using System.Collections.Generic;

/// <summary>
/// Configuration parameters for the BM25 algorithm.
/// </summary>
public class Bm25Parameters
{
    /// <summary>
    /// Gets or sets k1: Controls term frequency saturation point.
    /// Higher k1 = more impact from term frequency.
    /// Default: 1.5
    /// </summary>
    public double K1 { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets b: Controls how much document length normalizes scoring.
    /// b=0: No length normalization
    /// b=1: Full length normalization
    /// Default: 0.75
    /// </summary>
    public double B { get; set; } = 0.75;

    /// <summary>
    /// Gets or sets delta: Smoothing parameter.
    /// Prevents zero scores on sparse terms.
    /// Default: 0.5
    /// </summary>
    public double Delta { get; set; } = 0.5;

    /// <summary>
    /// Gets the average document length in the corpus (auto-calculated during indexing).
    /// </summary>
    public double AvgDocLength { get; private set; }

    /// <summary>
    /// Gets the default BM25 parameters (k1=1.5, b=0.75, delta=0.5).
    /// </summary>
    public static Bm25Parameters Default => new();

    /// <summary>
    /// Gets aggressive BM25 parameters (k1=2.0, b=1.0, delta=0.5).
    /// Use for large corpora with many long documents.
    /// </summary>
    public static Bm25Parameters Aggressive => new() { K1 = 2.0, B = 1.0 };

    /// <summary>
    /// Gets conservative BM25 parameters (k1=1.0, b=0.5, delta=0.5).
    /// Use for small corpora with consistent document length.
    /// </summary>
    public static Bm25Parameters Conservative => new() { K1 = 1.0, B = 0.5 };

    /// <summary>
    /// Sets the average document length (called internally during indexing).
    /// </summary>
    /// <param name="avgLength">The average document length.</param>
    internal void SetAvgDocLength(double avgLength)
    {
        AvgDocLength = avgLength;
    }
}
