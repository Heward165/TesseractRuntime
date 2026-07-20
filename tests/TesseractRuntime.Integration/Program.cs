using SixLabors.ImageSharp;
using SkiaSharp;
using TesseractRuntime;
using TesseractRuntime.Batching;
using TesseractRuntime.ImageSharp;
using TesseractRuntime.Pooling;
using TesseractRuntime.SkiaSharp;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: TesseractRuntime.Integration <tessdata> <image> <expected-text>");
    return 2;
}

using var source = await Image.LoadAsync(args[1]);
var grayImage = source.ToOcrImage();
var options = new OcrEngineOptions(
    args[0],
    "eng",
    variables: new Dictionary<string, string> { ["preserve_interword_spaces"] = "1" });
await using var pool = OcrEnginePool.Create(options, size: 2);
await using var lease = await pool.RentAsync();
var result = await lease.Engine.RecognizeAsync(
    grayImage,
    new OcrRequest
    {
        OutputFormats = OcrOutputFormat.Hocr | OcrOutputFormat.Tsv | OcrOutputFormat.AltoXml | OcrOutputFormat.PageXml,
        SourceResolution = 300,
        PageSegmentationMode = OcrPageSegmentationMode.Automatic,
    });

if (!result.Text.Contains(args[2], StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Expected '{args[2]}' but recognized:{Environment.NewLine}{result.Text}");
    return 1;
}

if (string.IsNullOrWhiteSpace(result.Hocr) ||
    string.IsNullOrWhiteSpace(result.Tsv) ||
    string.IsNullOrWhiteSpace(result.AltoXml) ||
    string.IsNullOrWhiteSpace(result.PageXml))
{
    Console.Error.WriteLine("Structured output was empty.");
    return 1;
}

var rgbPixels = new byte[checked(source.Width * source.Height * 3)];
using (var rgb = source.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgb24>())
{
    rgb.CopyPixelDataTo(rgbPixels);
}

var rgbaPixels = new byte[checked(source.Width * source.Height * 4)];
using (var rgba = source.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgba32>())
{
    rgba.CopyPixelDataTo(rgbaPixels);
}

foreach (var image in new[]
{
    OcrImage.CopyFrom(rgbPixels, source.Width, source.Height, 3),
    OcrImage.CopyFrom(rgbaPixels, source.Width, source.Height, 4),
})
{
    var pixelResult = await lease.Engine.RecognizeAsync(image, new OcrRequest { SourceResolution = 300 });
    if (!pixelResult.Text.Contains(args[2], StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("RGB/RGBA native recognition failed.");
        return 1;
    }
}

using (var skiaBitmap = new SKBitmap(
           new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul)))
{
    rgbaPixels.CopyTo(skiaBitmap.GetPixelSpan());
    var skiaResult = await lease.Engine.RecognizeAsync(
        skiaBitmap.ToOcrImage(),
        new OcrRequest { SourceResolution = 300 });
    if (!skiaResult.Text.Contains(args[2], StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("SkiaSharp native recognition failed.");
        return 1;
    }
}

var batch = new OcrBatchProcessor(pool);
var batchResults = await batch.ProcessAsync(
    [new OcrBatchItem("first", grayImage), new OcrBatchItem("second", grayImage)],
    maxDegreeOfParallelism: 2);
if (batchResults.Count != 2 || batchResults.Any(item => !item.IsSuccess))
{
    Console.Error.WriteLine("Pooled native batch failed.");
    return 1;
}

var detection = await OcrLanguageDetector.DetectAsync(
    grayImage,
    new Dictionary<string, OcrEnginePool> { ["eng"] = pool },
    new OcrRequest { SourceResolution = 300 });
if (detection.Language != "eng" || detection.Attempts.Count != 1 || !detection.Attempts[0].IsSuccess)
{
    Console.Error.WriteLine("Native language detection failed.");
    return 1;
}

await using var osdEngine = OcrEngine.Create(new OcrEngineOptions(
    args[0],
    "osd",
    OcrEngineMode.Default,
    variables: new Dictionary<string, string> { ["user_defined_dpi"] = "300" }));
var orientation = await osdEngine.DetectOrientationAsync(grayImage);
if (orientation is null)
{
    Console.Error.WriteLine("Orientation and script detection returned no result.");
    return 1;
}

Console.WriteLine($"Native version: {lease.Engine.Version}");
Console.WriteLine($"Native path: {TesseractRuntimeInfo.LoadedNativePath}");
Console.WriteLine($"Confidence: {result.MeanConfidence:P1}");
Console.WriteLine($"Orientation: {orientation.ClockwiseRotationDegrees}°, script {orientation.Script}");
Console.WriteLine("Native OCR integration passed.");
return 0;
