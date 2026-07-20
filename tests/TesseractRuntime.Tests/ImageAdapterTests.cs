using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TesseractRuntime.ImageSharp;
using SkiaSharp;
using TesseractRuntime.SkiaSharp;

namespace TesseractRuntime.Tests;

public sealed class ImageAdapterTests
{
    [Fact]
    public void ImageSharpAdapterProducesOwnedGray8Pixels()
    {
        using var source = new Image<Rgba32>(2, 1, Color.White);

        var converted = source.ToOcrImage();

        Assert.Equal(1, converted.BytesPerPixel);
        Assert.Equal(2, converted.Pixels.Length);
        Assert.All(converted.Pixels.ToArray(), value => Assert.Equal(255, value));
    }

    [Fact]
    public void ImageSharpAdapterRejectsNull()
    {
        Image? image = null;
        Assert.Throws<ArgumentNullException>(() => image!.ToOcrImage());
    }

    [Fact]
    public void ImageSharpAdapterDoesNotObserveLaterSourceMutation()
    {
        using var source = new Image<L8>(1, 1, new L8(10));
        var converted = source.ToOcrImage();

        source[0, 0] = new L8(200);

        Assert.Equal(10, converted.Pixels.Span[0]);
    }

    [Fact]
    public void SkiaSharpAdapterProducesRgbaPixels()
    {
        using var source = new SKBitmap(new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        source.SetPixel(0, 0, new SKColor(10, 20, 30, 255));

        var converted = source.ToOcrImage();

        Assert.Equal(4, converted.BytesPerPixel);
        Assert.Equal(4, converted.Pixels.Length);
        Assert.Equal(new byte[] { 10, 20, 30, 255 }, converted.Pixels.ToArray());
    }

    [Fact]
    public void SkiaSharpAdapterRejectsNullAndEmptyBitmap()
    {
        SKBitmap? missing = null;
        Assert.Throws<ArgumentNullException>(() => missing!.ToOcrImage());
        using var empty = new SKBitmap();
        Assert.Throws<ArgumentException>(() => empty.ToOcrImage());
    }
}
