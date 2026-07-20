using TesseractRuntime.Pooling;

namespace TesseractRuntime.Batching;

/// <summary>Runs ordered, bounded OCR batches using an engine pool.</summary>
public sealed class OcrBatchProcessor
{
    private readonly OcrEnginePool pool;

    public OcrBatchProcessor(OcrEnginePool pool)
    {
        this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
    }

    public async Task<IReadOnlyList<OcrBatchItemResult>> ProcessAsync(
        IEnumerable<OcrBatchItem> items,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        var input = items.ToArray();
        var parallelism = maxDegreeOfParallelism ?? pool.Size;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parallelism);
        parallelism = Math.Min(parallelism, pool.Size);

        var results = new OcrBatchItemResult[input.Length];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, input.Length),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = parallelism,
            },
            async (index, token) =>
            {
                var item = input[index];
                try
                {
                    await using var lease = await pool.RentAsync(token).ConfigureAwait(false);
                    var result = await lease.Engine.RecognizeAsync(item.Image, item.Request, token).ConfigureAwait(false);
                    results[index] = new OcrBatchItemResult(item.Id, result, null);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    results[index] = new OcrBatchItemResult(item.Id, null, exception);
                }
            }).ConfigureAwait(false);

        return results;
    }
}
