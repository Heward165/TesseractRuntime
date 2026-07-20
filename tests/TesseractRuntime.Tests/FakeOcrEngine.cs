namespace TesseractRuntime.Tests;

using System.Globalization;

internal sealed class FakeOcrEngine : IOcrEngine
{
    private readonly int? failOnPixel;
    private readonly TimeSpan delay;
    private readonly float confidence;
    private readonly ConcurrencyTracker? tracker;
    private int disposeCount;

    internal FakeOcrEngine(
        int? failOnPixel = null,
        TimeSpan delay = default,
        float confidence = 0.80F,
        ConcurrencyTracker? tracker = null)
    {
        this.failOnPixel = failOnPixel;
        this.delay = delay;
        this.confidence = confidence;
        this.tracker = tracker;
    }

    public string Version => "test";

    internal int DisposeCount => Volatile.Read(ref disposeCount);

    public async ValueTask<OcrResult> RecognizeAsync(
        OcrImage image,
        OcrRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request ??= new OcrRequest();
        request.ValidateForTests();
        var value = image.Pixels.Span[0];
        tracker?.Enter();
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (value == failOnPixel)
            {
                throw new InvalidDataException("Synthetic failure.");
            }

            return new OcrResult(value.ToString(CultureInfo.InvariantCulture), confidence, delay);
        }
        finally
        {
            tracker?.Exit();
        }
    }

    public ValueTask<OcrOrientation?> DetectOrientationAsync(
        OcrImage image,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<OcrOrientation?>(null);

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref disposeCount);
        return ValueTask.CompletedTask;
    }

    internal async Task ValidateRequestAsync(OcrImage image, OcrRequest request)
    {
        await RecognizeAsync(image, request);
    }
}

internal sealed class ConcurrencyTracker
{
    private int current;
    private int maximum;

    internal int Maximum => Volatile.Read(ref maximum);

    internal void Enter()
    {
        var observed = Interlocked.Increment(ref current);
        int previous;
        do
        {
            previous = Volatile.Read(ref maximum);
            if (observed <= previous)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref maximum, observed, previous) != previous);
    }

    internal void Exit() => Interlocked.Decrement(ref current);
}

internal static class OcrRequestTestExtensions
{
    internal static void ValidateForTests(this OcrRequest request)
    {
        if (request.SourceResolution is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
    }
}
