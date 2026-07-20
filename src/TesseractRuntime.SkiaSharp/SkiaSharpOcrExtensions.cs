using SkiaSharp;

namespace TesseractRuntime.SkiaSharp;

/// <summary>Converts Skia bitmaps to a predictable RGBA8888 buffer for native recognition.</summary>
public static class SkiaSharpOcrExtensions
{
    public static OcrImage ToOcrImage(this SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new ArgumentException("The bitmap must contain pixels.", nameof(bitmap));
        }

        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var converted = new SKBitmap(info);
        using (var canvas = new SKCanvas(converted))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(
                bitmap,
                new SKRect(0, 0, bitmap.Width, bitmap.Height),
                new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None));
            canvas.Flush();
        }

        return OcrImage.CopyFrom(
            converted.GetPixelSpan(),
            converted.Width,
            converted.Height,
            bytesPerPixel: 4,
            converted.RowBytes);
    }
}
