namespace TesseractRuntime;

/// <summary>
/// An immutable, tightly-owned pixel buffer. Copying at the boundary prevents a caller from
/// mutating memory while native recognition is in progress.
/// </summary>
public sealed class OcrImage
{
    private readonly byte[] pixels;

    private OcrImage(byte[] pixels, int width, int height, int bytesPerPixel, int stride)
    {
        this.pixels = pixels;
        Width = width;
        Height = height;
        BytesPerPixel = bytesPerPixel;
        Stride = stride;
    }

    public int Width { get; }

    public int Height { get; }

    public int BytesPerPixel { get; }

    public int Stride { get; }

    public ReadOnlyMemory<byte> Pixels => pixels;

    /// <summary>Copies an 8-bit grayscale, 24-bit RGB, or 32-bit RGBA buffer.</summary>
    public static OcrImage CopyFrom(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int bytesPerPixel,
        int? stride = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (bytesPerPixel is not (1 or 3 or 4))
        {
            throw new ArgumentOutOfRangeException(
                nameof(bytesPerPixel),
                bytesPerPixel,
                "TesseractRuntime accepts Gray8, RGB24, or RGBA32 buffers.");
        }

        var minimumStride = checked(width * bytesPerPixel);
        var actualStride = stride ?? minimumStride;
        if (actualStride < minimumStride)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), actualStride, "Stride is smaller than one pixel row.");
        }

        var requiredLength = checked(actualStride * height);
        if (pixels.Length < requiredLength)
        {
            throw new ArgumentException(
                $"The pixel buffer contains {pixels.Length} bytes; {requiredLength} are required.",
                nameof(pixels));
        }

        return new OcrImage(pixels[..requiredLength].ToArray(), width, height, bytesPerPixel, actualStride);
    }
}
