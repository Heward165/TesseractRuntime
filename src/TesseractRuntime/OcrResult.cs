namespace TesseractRuntime;

/// <summary>The managed result of a completed native recognition pass.</summary>
public sealed record OcrResult(
    string Text,
    float MeanConfidence,
    TimeSpan Duration,
    string? Hocr = null,
    string? Tsv = null,
    string? AltoXml = null,
    string? PageXml = null);
