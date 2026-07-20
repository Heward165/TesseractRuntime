namespace TesseractRuntime.Batching;

/// <summary>A successful or failed item; a bad image does not erase other batch results.</summary>
public sealed record OcrBatchItemResult(string Id, OcrResult? Result, Exception? Error)
{
    public bool IsSuccess => Error is null;
}
