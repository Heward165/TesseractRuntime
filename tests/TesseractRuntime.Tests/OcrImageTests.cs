using TesseractRuntime;

namespace TesseractRuntime.Tests;

public sealed class OcrImageTests
{
    [Fact]
    public void CopyFromOwnsThePixelBuffer()
    {
        byte[] source = [1, 2, 3, 4];
        var image = OcrImage.CopyFrom(source, 2, 2, 1);

        source[0] = 99;

        Assert.Equal(1, image.Pixels.Span[0]);
        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Stride);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void CopyFromRejectsUnsupportedPixelFormats(int bytesPerPixel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OcrImage.CopyFrom(new byte[16], 1, 1, bytesPerPixel));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(3, 1, 3)]
    [InlineData(4, 1, 4)]
    public void CopyFromAcceptsEverySupportedPixelFormat(int bytesPerPixel, int width, int length)
    {
        var image = OcrImage.CopyFrom(new byte[length], width, 1, bytesPerPixel);

        Assert.Equal(bytesPerPixel, image.BytesPerPixel);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void CopyFromRejectsNonPositiveDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OcrImage.CopyFrom([0], width, height, 1));
    }

    [Fact]
    public void CopyFromDetectsIntegerOverflow()
    {
        Assert.Throws<OverflowException>(() => OcrImage.CopyFrom([], int.MaxValue, 2, 4));
    }

    [Fact]
    public void CopyFromRejectsShortRows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OcrImage.CopyFrom(new byte[12], 2, 2, 3, stride: 5));
    }

    [Fact]
    public void CopyFromRejectsShortBuffer()
    {
        Assert.Throws<ArgumentException>(() => OcrImage.CopyFrom(new byte[3], 2, 2, 1));
    }

    [Fact]
    public void CopyFromPreservesPadding()
    {
        var image = OcrImage.CopyFrom(new byte[8], 3, 2, 1, stride: 4);

        Assert.Equal(4, image.Stride);
        Assert.Equal(8, image.Pixels.Length);
    }
}
