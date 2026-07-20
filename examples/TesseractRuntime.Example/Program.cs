using SixLabors.ImageSharp;
using TesseractRuntime;
using TesseractRuntime.ImageSharp;
using TesseractRuntime.Pooling;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: dotnet run -- <image-path> <tessdata-path> [language]");
    return 2;
}

var language = args.Length == 3 ? args[2] : "eng";
using var source = await Image.LoadAsync(args[0]);
var image = source.ToOcrImage();

await using var pool = OcrEnginePool.Create(new OcrEngineOptions(args[1], language), size: 2);
await using var lease = await pool.RentAsync();
var result = await lease.Engine.RecognizeAsync(
    image,
    new OcrRequest { OutputFormats = OcrOutputFormat.Tsv, SourceResolution = 300 });

Console.WriteLine(result.Text.Trim());
Console.WriteLine($"Confidence: {result.MeanConfidence:P1}");
Console.WriteLine($"Engine: {lease.Engine.Version}");
return 0;
