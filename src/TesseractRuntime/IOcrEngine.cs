namespace TesseractRuntime;

/// <summary>A single, non-concurrent recognition engine suitable for pooling.</summary>
public interface IOcrEngine : IAsyncDisposable
{
    string Version { get; }

    ValueTask<OcrResult> RecognizeAsync(
        OcrImage image,
        OcrRequest? request = null,
        CancellationToken cancellationToken = default);

    ValueTask<OcrOrientation?> DetectOrientationAsync(
        OcrImage image,
        CancellationToken cancellationToken = default);
}
