using TesseractRuntime.Batching;
using TesseractRuntime.Pooling;
using System.Globalization;

namespace TesseractRuntime.Tests;

public sealed class PoolAndBatchTests
{
    [Fact]
    public async Task PoolBoundsConcurrentLeases()
    {
        var token = TestContext.Current.CancellationToken;
        await using var pool = OcrEnginePool.Create(2, () => new FakeOcrEngine());
        await using var first = await pool.RentAsync(token);
        await using var second = await pool.RentAsync(token);

        var thirdTask = pool.RentAsync(token).AsTask();
        await Task.Delay(50, token);
        Assert.False(thirdTask.IsCompleted);

        await first.DisposeAsync();
        await using var third = await thirdTask;
        Assert.NotNull(third.Engine);
    }

    [Fact]
    public async Task LeaseReturnIsIdempotent()
    {
        var token = TestContext.Current.CancellationToken;
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine());
        var lease = await pool.RentAsync(token);

        await lease.DisposeAsync();
        await lease.DisposeAsync();

        await using var next = await pool.RentAsync(token);
        Assert.NotNull(next.Engine);
    }

    [Fact]
    public async Task BatchPreservesInputOrderAndPartialFailures()
    {
        var token = TestContext.Current.CancellationToken;
        await using var pool = OcrEnginePool.Create(2, () => new FakeOcrEngine(failOnPixel: 2));
        var processor = new OcrBatchProcessor(pool);
        var items = Enumerable.Range(1, 3)
            .Select(value => new OcrBatchItem(value.ToString(CultureInfo.InvariantCulture), Pixel(value)))
            .ToArray();

        var results = await processor.ProcessAsync(items, cancellationToken: token);

        Assert.Equal(["1", "2", "3"], results.Select(result => result.Id));
        Assert.True(results[0].IsSuccess);
        Assert.False(results[1].IsSuccess);
        Assert.IsType<InvalidDataException>(results[1].Error);
        Assert.True(results[2].IsSuccess);
    }

    [Fact]
    public async Task BatchNeverExceedsRequestedParallelism()
    {
        var token = TestContext.Current.CancellationToken;
        var tracker = new ConcurrencyTracker();
        await using var pool = OcrEnginePool.Create(4, () => new FakeOcrEngine(delay: TimeSpan.FromMilliseconds(20), tracker: tracker));
        var processor = new OcrBatchProcessor(pool);
        var items = Enumerable.Range(1, 8).Select(value => new OcrBatchItem(value.ToString(CultureInfo.InvariantCulture), Pixel(value)));

        await processor.ProcessAsync(items, maxDegreeOfParallelism: 2, token);

        Assert.InRange(tracker.Maximum, 1, 2);
    }

    [Fact]
    public async Task LanguageDetectionSelectsHighestConfidence()
    {
        var token = TestContext.Current.CancellationToken;
        await using var english = OcrEnginePool.Create(1, () => new FakeOcrEngine(confidence: 0.70F));
        await using var portuguese = OcrEnginePool.Create(1, () => new FakeOcrEngine(confidence: 0.91F));
        var candidates = new Dictionary<string, OcrEnginePool>
        {
            ["eng"] = english,
            ["por"] = portuguese,
        };

        var detected = await OcrLanguageDetector.DetectAsync(Pixel(1), candidates, cancellationToken: token);

        Assert.Equal("por", detected.Language);
        Assert.Equal(2, detected.Attempts.Count);
    }

    [Fact]
    public void NativeCandidatesIncludeCurrentRuntimeIdentifier()
    {
        Assert.Contains(
            TesseractRuntimeInfo.NativeLibraryCandidates,
            candidate => candidate.Contains(System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, StringComparison.Ordinal));
    }

    private static OcrImage Pixel(int value) => OcrImage.CopyFrom([(byte)value], 1, 1, 1);
}
