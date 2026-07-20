using SixLabors.ImageSharp;
using TesseractRuntime;
using TesseractRuntime.ImageSharp;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: TesseractRuntime.PackageSmoke <tessdata> <image> <expected-text>");
    return 2;
}

using var source = await Image.LoadAsync(args[1]);
await using var engine = OcrEngine.Create(new OcrEngineOptions(args[0], "eng"));
var result = await engine.RecognizeAsync(
    source.ToOcrImage(),
    new OcrRequest { InputName = Path.GetFileName(args[1]), SourceResolution = 300 });

if (!result.Text.Contains(args[2], StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Expected '{args[2]}' but recognized:{Environment.NewLine}{result.Text}");
    return 1;
}

var loadedPath = TesseractRuntimeInfo.LoadedNativePath;
if (string.IsNullOrWhiteSpace(loadedPath) || !Path.IsPathFullyQualified(loadedPath))
{
    Console.Error.WriteLine("The packaged native runtime was not resolved to an absolute path.");
    return 1;
}

Console.WriteLine($"Package smoke passed with Tesseract {engine.Version} from {loadedPath}.");
return 0;
