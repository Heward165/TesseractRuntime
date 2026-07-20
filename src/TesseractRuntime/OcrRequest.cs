namespace TesseractRuntime;

/// <summary>Settings that can vary safely between recognition calls.</summary>
public sealed record OcrRequest
{
    public OcrOutputFormat OutputFormats { get; init; } = OcrOutputFormat.None;

    public OcrPageSegmentationMode? PageSegmentationMode { get; init; }

    public int? SourceResolution { get; init; }

    public int PageNumber { get; init; }

    /// <summary>A logical source name used in structured output; defaults to <c>memory</c>.</summary>
    public string InputName { get; init; } = "memory";

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(InputName);
        if (SourceResolution is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SourceResolution), SourceResolution, "Resolution must be positive.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(PageNumber);
    }
}
