using TesseractRuntime.Pooling;

namespace TesseractRuntime;

/// <summary>
/// Explicitly evaluates candidate language pools. This is intentionally opt-in because language
/// detection requires multiple OCR passes and cannot be inferred cheaply before recognition.
/// </summary>
public static class OcrLanguageDetector
{
    public static async Task<OcrLanguageDetectionResult> DetectAsync(
        OcrImage image,
        IReadOnlyDictionary<string, OcrEnginePool> candidates,
        OcrRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate language pool is required.", nameof(candidates));
        }

        var attempts = await Task.WhenAll(candidates.Select(async candidate =>
        {
            try
            {
                await using var lease = await candidate.Value.RentAsync(cancellationToken).ConfigureAwait(false);
                var result = await lease.Engine.RecognizeAsync(image, request, cancellationToken).ConfigureAwait(false);
                return new OcrLanguageAttempt(candidate.Key, result, null);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new OcrLanguageAttempt(candidate.Key, null, exception);
            }
        })).ConfigureAwait(false);

        // Confidence is the primary signal. Text length breaks ties because empty/noisy passes can
        // occasionally report the same rounded mean confidence as a useful result.
        var successful = attempts.Where(attempt => attempt.IsSuccess).ToArray();
        if (successful.Length == 0)
        {
            throw new AggregateException("Every candidate language failed.", attempts.Select(attempt => attempt.Error!));
        }

        var best = successful
            .OrderByDescending(attempt => attempt.Result!.MeanConfidence)
            .ThenByDescending(attempt => attempt.Result!.Text.Trim().Length)
            .First();

        return new OcrLanguageDetectionResult(best.Language, best.Result!, attempts);
    }
}

public sealed record OcrLanguageAttempt(string Language, OcrResult? Result, Exception? Error)
{
    public bool IsSuccess => Error is null && Result is not null;
}

public sealed record OcrLanguageDetectionResult(
    string Language,
    OcrResult Result,
    IReadOnlyList<OcrLanguageAttempt> Attempts);
