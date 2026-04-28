namespace ElBruno.BM25;

using System;

/// <summary>
/// Enum specifying the metric used to evaluate BM25 parameter tuning.
/// </summary>
public enum TuningMetric
{
    /// <summary>
    /// Precision: What fraction of retrieved documents are relevant.
    /// </summary>
    Precision,

    /// <summary>
    /// Recall: What fraction of relevant documents are retrieved.
    /// </summary>
    Recall,

    /// <summary>
    /// F1 Score: Harmonic mean of precision and recall.
    /// </summary>
    F1,

    /// <summary>
    /// Normalized Discounted Cumulative Gain: Measures ranking quality.
    /// </summary>
    NDCG
}

/// <summary>
/// Result of a single parameter tuning attempt.
/// Contains the parameters tested and the metric value achieved.
/// </summary>
public class ParameterTuneResult
{
    /// <summary>
    /// Gets or sets the BM25 parameters used in this tuning attempt.
    /// </summary>
    public required Bm25Parameters Parameters { get; set; }

    /// <summary>
    /// Gets or sets the metric value achieved with these parameters.
    /// The metric type is determined by the tuning process.
    /// </summary>
    public double Metric { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this tuning was performed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets notes about this tuning result.
    /// </summary>
    public string? Notes { get; set; }
}
