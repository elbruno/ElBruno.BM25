namespace ElBruno.BM25;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Optimizes BM25 parameters for a given index using validation queries.
/// Provides grid search and automatic tuning capabilities.
/// </summary>
/// <typeparam name="T">The type of documents in the index.</typeparam>
public class Bm25Tuner<T>
{
    private readonly Bm25Index<T> _index;

    /// <summary>
    /// Initializes a new instance of the Bm25Tuner class.
    /// </summary>
    /// <param name="index">The BM25 index to tune parameters for.</param>
    /// <exception cref="ArgumentNullException">Thrown when index is null.</exception>
    public Bm25Tuner(Bm25Index<T> index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>
    /// Automatically tunes BM25 parameters using a validation set of queries and relevant documents.
    /// Performs grid search over K1 and B parameter space, optimizing for the specified metric.
    /// </summary>
    /// <param name="validationQueries">List of (query, relevant_documents) tuples for tuning validation.</param>
    /// <param name="metric">The metric to optimize for (default: Recall).</param>
    /// <param name="ct">Cancellation token for long-running operations.</param>
    /// <returns>The optimized Bm25Parameters that achieved the best metric on the validation set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when validationQueries is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validationQueries is empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<Bm25Parameters> TuneAsync(
        List<(string query, List<T> relevantDocs)> validationQueries,
        TuningMetric metric = TuningMetric.Recall,
        CancellationToken ct = default
    )
    {
        if (validationQueries == null) throw new ArgumentNullException(nameof(validationQueries));
        if (validationQueries.Count == 0) throw new ArgumentException("Validation queries cannot be empty.", nameof(validationQueries));

        var results = await GridSearchAsync(validationQueries, ct);
        ct.ThrowIfCancellationRequested();

        var bestResult = results
            .OrderByDescending(r =>
            {
                return metric switch
                {
                    TuningMetric.Precision => CalculatePrecision(validationQueries, r.Parameters, ct),
                    TuningMetric.Recall => CalculateRecall(validationQueries, r.Parameters, ct),
                    TuningMetric.F1 => CalculateF1(validationQueries, r.Parameters, ct),
                    TuningMetric.NDCG => CalculateNDCG(validationQueries, r.Parameters, ct),
                    _ => r.Metric
                };
            })
            .First();

        return bestResult.Parameters;
    }

    /// <summary>
    /// Performs grid search over a parameter space of K1 and B values.
    /// Tests combinations of K1 from 0.5 to 2.5 and B from 0 to 1.0.
    /// </summary>
    /// <param name="validationQueries">List of (query, relevant_documents) tuples for evaluation.</param>
    /// <param name="ct">Cancellation token for long-running operations.</param>
    /// <returns>A list of ParameterTuneResult objects showing metric values for each parameter combination.</returns>
    /// <exception cref="ArgumentNullException">Thrown when validationQueries is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validationQueries is empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<List<ParameterTuneResult>> GridSearchAsync(
        List<(string query, List<T> relevantDocs)> validationQueries,
        CancellationToken ct = default
    )
    {
        if (validationQueries == null) throw new ArgumentNullException(nameof(validationQueries));
        if (validationQueries.Count == 0) throw new ArgumentException("Validation queries cannot be empty.", nameof(validationQueries));

        var results = new List<ParameterTuneResult>();
        var originalParams = _index.Parameters;

        try
        {
            // Grid search ranges
            var k1Values = new[] { 0.5, 1.0, 1.5, 2.0, 2.5 };
            var bValues = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };

            var totalCombinations = k1Values.Length * bValues.Length;
            var currentCombination = 0;

            await Task.Run(() =>
            {
                foreach (var k1 in k1Values)
                {
                    foreach (var b in bValues)
                    {
                        ct.ThrowIfCancellationRequested();

                        var testParams = new Bm25Parameters { K1 = k1, B = b, Delta = 0.5 };
                        testParams.SetAvgDocLength(_index.Parameters.AvgDocLength);
                        _index.Parameters = testParams;

                        var recall = CalculateRecall(validationQueries, testParams, ct);
                        var precision = CalculatePrecision(validationQueries, testParams, ct);
                        var f1 = CalculateF1(validationQueries, testParams, ct);
                        var ndcg = CalculateNDCG(validationQueries, testParams, ct);

                        results.Add(new ParameterTuneResult
                        {
                            Parameters = new Bm25Parameters { K1 = k1, B = b, Delta = 0.5 },
                            Metric = recall,
                            Notes = $"K1={k1:F2}, B={b:F2} - Precision={precision:F4}, Recall={recall:F4}, F1={f1:F4}, NDCG={ndcg:F4}"
                        });

                        currentCombination++;
                    }
                }
            }, ct);

            return results;
        }
        finally
        {
            _index.Parameters = originalParams;
        }
    }

    /// <summary>
    /// Calculates recall on the validation set for given parameters.
    /// Recall = (relevant docs retrieved) / (total relevant docs)
    /// </summary>
    private double CalculateRecall(List<(string query, List<T> relevantDocs)> validationQueries, Bm25Parameters parameters, CancellationToken ct)
    {
        var recalls = new List<double>();

        foreach (var (query, relevantDocs) in validationQueries)
        {
            ct.ThrowIfCancellationRequested();

            if (relevantDocs.Count == 0) continue;

            var results = _index.Search(query, topK: Math.Max(10, relevantDocs.Count * 2), ct: ct);
            var retrievedRelevant = results.Select(r => r.document).Intersect(relevantDocs).Count();
            recalls.Add((double)retrievedRelevant / relevantDocs.Count);
        }

        return recalls.Count > 0 ? recalls.Average() : 0;
    }

    /// <summary>
    /// Calculates precision on the validation set for given parameters.
    /// Precision = (relevant docs retrieved) / (total docs retrieved)
    /// </summary>
    private double CalculatePrecision(List<(string query, List<T> relevantDocs)> validationQueries, Bm25Parameters parameters, CancellationToken ct)
    {
        var precisions = new List<double>();

        foreach (var (query, relevantDocs) in validationQueries)
        {
            ct.ThrowIfCancellationRequested();

            var results = _index.Search(query, topK: 10, ct: ct);
            if (results.Count == 0) continue;

            var retrievedRelevant = results.Select(r => r.document).Intersect(relevantDocs).Count();
            precisions.Add((double)retrievedRelevant / results.Count);
        }

        return precisions.Count > 0 ? precisions.Average() : 0;
    }

    /// <summary>
    /// Calculates F1 score on the validation set for given parameters.
    /// F1 = 2 * (Precision * Recall) / (Precision + Recall)
    /// </summary>
    private double CalculateF1(List<(string query, List<T> relevantDocs)> validationQueries, Bm25Parameters parameters, CancellationToken ct)
    {
        var precision = CalculatePrecision(validationQueries, parameters, ct);
        var recall = CalculateRecall(validationQueries, parameters, ct);

        if (precision + recall == 0) return 0;
        return 2 * (precision * recall) / (precision + recall);
    }

    /// <summary>
    /// Calculates Normalized Discounted Cumulative Gain (NDCG) on the validation set.
    /// NDCG measures ranking quality, giving higher weight to relevant documents at higher ranks.
    /// </summary>
    private double CalculateNDCG(List<(string query, List<T> relevantDocs)> validationQueries, Bm25Parameters parameters, CancellationToken ct)
    {
        var ndcgScores = new List<double>();

        foreach (var (query, relevantDocs) in validationQueries)
        {
            ct.ThrowIfCancellationRequested();

            var results = _index.Search(query, topK: 10, ct: ct);
            if (results.Count == 0) continue;

            double dcg = 0;
            double idcg = 0;

            for (int i = 0; i < results.Count; i++)
            {
                var isRelevant = relevantDocs.Contains(results[i].document) ? 1 : 0;
                dcg += isRelevant / Math.Log(i + 2, 2);
            }

            for (int i = 0; i < Math.Min(relevantDocs.Count, results.Count); i++)
            {
                idcg += 1.0 / Math.Log(i + 2, 2);
            }

            var ndcg = idcg > 0 ? dcg / idcg : 0;
            ndcgScores.Add(ndcg);
        }

        return ndcgScores.Count > 0 ? ndcgScores.Average() : 0;
    }
}
