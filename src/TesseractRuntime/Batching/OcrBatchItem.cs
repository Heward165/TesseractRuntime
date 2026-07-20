namespace TesseractRuntime.Batching;

/// <summary>One independently identifiable unit of batch work.</summary>
public sealed record OcrBatchItem(string Id, OcrImage Image, OcrRequest? Request = null)
{
    public string Id { get; } = string.IsNullOrWhiteSpace(Id)
        ? throw new ArgumentException("A batch item ID is required.", nameof(Id))
        : Id;

    public OcrImage Image { get; } = Image ?? throw new ArgumentNullException(nameof(Image));
}
