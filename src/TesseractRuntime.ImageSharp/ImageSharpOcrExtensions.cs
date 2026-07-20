using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TesseractRuntime.ImageSharp;

/// <summary>Converts ImageSharp images to the core library's owned Gray8 representation.</summary>
public static class ImageSharpOcrExtensions
{
    public static OcrImage ToOcrImage(this Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var grayscale = image.CloneAs<L8>();
        var pixels = new byte[checked(grayscale.Width * grayscale.Height)];
        grayscale.CopyPixelDataTo(pixels);
        return OcrImage.CopyFrom(pixels, grayscale.Width, grayscale.Height, bytesPerPixel: 1);
    }
}
