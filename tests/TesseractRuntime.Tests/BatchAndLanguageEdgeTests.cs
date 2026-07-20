using TesseractRuntime.Batching;
using TesseractRuntime.Pooling;

namespace TesseractRuntime.Tests;

public sealed class BatchAndLanguageEdgeTests
{
    [Fact]
    public void BatchItemValidatesIdentityAndImage()
    {
        var image = Pixel(1);

        Assert.Throws<ArgumentException>(() => new OcrBatchItem(" ", image));
        Assert.Throws<ArgumentNullException>(() => new OcrBatchItem("id", null!));
    }

    [Fact]
    public async Task EmptyBatchReturnsEmptyResult()
    {
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine());
        var processor = new OcrBatchProcessor(pool);

        var results = await processor.ProcessAsync([], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task BatchRejectsNullItemsAndInvalidParallelism()
    {
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine());
        var processor = new OcrBatchProcessor(pool);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await processor.ProcessAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await processor.ProcessAsync([], 0, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void BatchProcessorRejectsNullPool()
    {
        Assert.Throws<ArgumentNullException>(() => new OcrBatchProcessor(null!));
    }

    [Fact]
    public async Task BatchPropagatesProcessCancellation()
    {
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine(delay: TimeSpan.FromSeconds(1)));
        var processor = new OcrBatchProcessor(pool);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await processor.ProcessAsync([new OcrBatchItem("id", Pixel(1))], cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task LanguageDetectionRejectsEmptyCandidates()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await OcrLanguageDetector.DetectAsync(
                Pixel(1),
                new Dictionary<string, OcrEnginePool>(),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LanguageDetectionKeepsFailedAttemptsAndSelectsSuccess()
    {
        await using var failed = OcrEnginePool.Create(1, () => new FakeOcrEngine(failOnPixel: 1));
        await using var success = OcrEnginePool.Create(1, () => new FakeOcrEngine(confidence: 0.6F));
        var candidates = new Dictionary<string, OcrEnginePool> { ["failed"] = failed, ["success"] = success };

        var result = await OcrLanguageDetector.DetectAsync(
            Pixel(1),
            candidates,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("success", result.Language);
        Assert.False(result.Attempts.Single(attempt => attempt.Language == "failed").IsSuccess);
        Assert.IsType<InvalidDataException>(result.Attempts.Single(attempt => attempt.Language == "failed").Error);
    }

    [Fact]
    public async Task LanguageDetectionAggregatesWhenEveryCandidateFails()
    {
        await using var first = OcrEnginePool.Create(1, () => new FakeOcrEngine(failOnPixel: 1));
        await using var second = OcrEnginePool.Create(1, () => new FakeOcrEngine(failOnPixel: 1));

        var error = await Assert.ThrowsAsync<AggregateException>(async () =>
            await OcrLanguageDetector.DetectAsync(
                Pixel(1),
                new Dictionary<string, OcrEnginePool> { ["one"] = first, ["two"] = second },
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(2, error.InnerExceptions.Count);
    }

    [Fact]
    public async Task LanguageDetectionPropagatesCancellationInsteadOfRecordingFailure()
    {
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine(delay: TimeSpan.FromSeconds(1)));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await OcrLanguageDetector.DetectAsync(
                Pixel(1),
                new Dictionary<string, OcrEnginePool> { ["eng"] = pool },
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public void LanguageAttemptRequiresBothResultAndNoErrorForSuccess()
    {
        var result = new OcrResult("text", 0.5F, TimeSpan.FromMilliseconds(1));

        Assert.True(new OcrLanguageAttempt("eng", result, null).IsSuccess);
        Assert.False(new OcrLanguageAttempt("eng", null, null).IsSuccess);
        Assert.False(new OcrLanguageAttempt("eng", result, new InvalidOperationException()).IsSuccess);
    }

    [Fact]
    public void ResultRecordsExposeEveryValue()
    {
        var duration = TimeSpan.FromMilliseconds(12);
        var result = new OcrResult("text", 0.7F, duration, "hocr", "tsv", "alto", "page");
        var orientation = new OcrOrientation(270, 4.2F, "Cyrillic", 3.1F);
        var detection = new OcrLanguageDetectionResult("eng", result, [new OcrLanguageAttempt("eng", result, null)]);

        Assert.Equal(duration, result.Duration);
        Assert.Equal(270, orientation.ClockwiseRotationDegrees);
        Assert.Equal(4.2F, orientation.OrientationConfidence);
        Assert.Equal("Cyrillic", orientation.Script);
        Assert.Equal(3.1F, orientation.ScriptConfidence);
        Assert.Same(result, detection.Result);
        Assert.Single(detection.Attempts);
    }

    [Fact]
    public async Task LanguageDetectionBreaksConfidenceTieWithUsefulTextLength()
    {
        await using var shortText = OcrEnginePool.Create(1, () => new FixedTextEngine("x", 0.8F));
        await using var longText = OcrEnginePool.Create(1, () => new FixedTextEngine("useful text", 0.8F));

        var result = await OcrLanguageDetector.DetectAsync(
            Pixel(1),
            new Dictionary<string, OcrEnginePool> { ["short"] = shortText, ["long"] = longText },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("long", result.Language);
    }

    private static OcrImage Pixel(int value) => OcrImage.CopyFrom([(byte)value], 1, 1, 1);
}

internal sealed class FixedTextEngine(string text, float confidence) : IOcrEngine
{
    public string Version => "test";

    public ValueTask<OcrResult> RecognizeAsync(
        OcrImage image,
        OcrRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new OcrResult(text, confidence, TimeSpan.Zero));

    public ValueTask<OcrOrientation?> DetectOrientationAsync(
        OcrImage image,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<OcrOrientation?>(null);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
