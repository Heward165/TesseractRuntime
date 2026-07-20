# TesseractRuntime

TesseractRuntime is a modern, offline OCR runtime for .NET. It combines a small C# 14 binding to
Tesseract's stable C API with reproducible native builds, safe image ownership, engine pooling,
bounded batch execution, and optional image-library adapters.

The project is an independent clean-room implementation. It is not a fork of
`charlesw/tesseract` and does not share its source history.

## Why this project exists

OCR is not built into cross-platform .NET. Cloud OCR can be excellent, but it does not replace
local recognition when documents are private, connectivity is unavailable, or latency must stay
inside the process. The difficult part is not calling one native function; it is maintaining the
Tesseract/Leptonica/codec dependency matrix across operating systems and CPU architectures.

TesseractRuntime treats that native supply chain as part of the product.

## Package family

| Package | Purpose |
| --- | --- |
| `TesseractRuntime` | Dependency-free managed API, native resolver, pooling, batching, OSD, and candidate-language evaluation |
| `TesseractRuntime.ImageSharp` | Optional ImageSharp 3 adapter |
| `TesseractRuntime.SkiaSharp` | Optional SkiaSharp 4 adapter |
| `TesseractRuntime.Native.<rid>` | Tesseract 5.5.2 and its native runtime dependency closure for one RID |

Native build targets are `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and
`osx-arm64`. Windows packages statically link the C runtime, so they do not depend on the old
Visual C++ 2019 redistributable used by older wrappers.

> **Status:** `0.1.0-preview.1`. The API and build matrix are deliberately preview quality until
> every native package has accumulated production feedback.

## Quick start

Install the managed package, one native package matching the deployment RID, and an image adapter:

```bash
dotnet add package TesseractRuntime --prerelease
dotnet add package TesseractRuntime.Native.win-x64 --prerelease
dotnet add package TesseractRuntime.ImageSharp --prerelease
```

Download `eng.traineddata` from an official Tesseract tessdata repository and place it in
`./tessdata`, then:

```csharp
using SixLabors.ImageSharp;
using TesseractRuntime;
using TesseractRuntime.ImageSharp;
using TesseractRuntime.Pooling;

using var source = await Image.LoadAsync("scan.png");
var image = source.ToOcrImage();

var options = new OcrEngineOptions("./tessdata", "eng");
await using var pool = OcrEnginePool.Create(options, size: 2);
await using var lease = await pool.RentAsync();

var result = await lease.Engine.RecognizeAsync(
    image,
    new OcrRequest
    {
        SourceResolution = 300,
        OutputFormats = OcrOutputFormat.Hocr | OcrOutputFormat.Tsv,
    });

Console.WriteLine(result.Text);
Console.WriteLine($"Confidence: {result.MeanConfidence:P1}");
```

`OcrImage.CopyFrom` also accepts owned copies of Gray8, RGB24, and RGBA32 buffers, so the core has
no dependency on `System.Drawing`, ImageSharp, or SkiaSharp.

## Pooling and batch processing

A native engine is expensive to initialize and is not safe for concurrent calls. The pool has a
fixed capacity, uses asynchronous leases, and waits for outstanding leases during disposal.

```csharp
var processor = new OcrBatchProcessor(pool);
var items = files.Select(file => new OcrBatchItem(file.Name, file.Image));
var results = await processor.ProcessAsync(items, maxDegreeOfParallelism: 2, cancellationToken);

foreach (var item in results)
{
    Console.WriteLine(item.IsSuccess ? item.Result!.Text : item.Error!.Message);
}
```

Results retain input order. One corrupt image becomes one failed item instead of discarding the
rest of the batch. Parallelism is bounded by both the request and pool capacity.

## Language and orientation detection

Language selection is explicit because Tesseract normally needs a model before recognition.
`OcrLanguageDetector` evaluates caller-selected language pools and returns every attempt plus the
highest-confidence result. This costs one OCR pass per candidate and should not be presented as a
free automatic feature.

Orientation and script detection uses Tesseract OSD through `DetectOrientationAsync`. Initialize
that engine with the official `osd.traineddata` model. Sparse pages may not contain enough evidence
for a reliable estimate, in which case the method returns `null`.

## Native resolution

The resolver searches in this order:

1. `TESSERACT_RUNTIME_NATIVE_PATH` (a library or directory);
2. `runtimes/<current-rid>/native` beside the application;
3. the application directory;
4. the operating system loader.

`TesseractRuntimeInfo.NativeLibraryCandidates`, `LoadedNativePath`, and `GetNativeVersion()` make
deployment failures diagnosable. Windows dependencies are resolved beside the selected library
without changing the process-wide `PATH`.

## Important behavior

- Tesseract 5.x is required; native 5.5.2 is the pinned distribution version.
- PAGE XML requires a native build that exports `TessBaseAPIGetPAGEText`; every bundled 5.5.2
  package is tested for it, while older system Tesseract packages may not provide that export.
- Each `OcrImage` owns its pixels. This intentionally trades one copy for native memory safety.
- Buffer recognition assigns the logical input name `memory` by default. Set `OcrRequest.InputName`
  when structured XML should contain the original document name.
- Cancellation is observed while waiting for an engine and before/after native recognition. The
  stable Tesseract C API cannot safely interrupt a recognition call already executing.
- Language models are not bundled because their size and licensing/release cadence differ from the
  engine. Missing models are reported before native initialization.
- Native packages include dependency license notices generated from the pinned vcpkg build.

## Build and test

```bash
dotnet restore TesseractRuntime.slnx
dotnet format TesseractRuntime.slnx --verify-no-changes --no-restore
dotnet build TesseractRuntime.slnx -c Release --no-restore
dotnet test tests/TesseractRuntime.Tests/TesseractRuntime.Tests.csproj -c Release --no-build
dotnet run --project tests/TesseractRuntime.Conformance -c Release --no-build
```

The normal CI matrix runs on Windows, Linux, and macOS and performs a real OCR smoke test on Linux.
The separate native matrix builds and exercises all six RIDs before producing native NuGet
artifacts. The managed policy surface is gated at 96% line and branch coverage; generated/native
interop is verified by real OCR instead. See [verification strategy](docs/testing.md) and
[native distribution](docs/native-distribution.md) for the complete release gates.

## Documentation

- [Architecture](docs/architecture.md)
- [Verification strategy](docs/testing.md)
- [Native distribution](docs/native-distribution.md)
- [Clean-room record](docs/clean-room.md)
- [Roadmap](docs/roadmap.md)
- [Security policy](SECURITY.md)

## License

The managed code is MIT licensed. Tesseract is Apache-2.0, Leptonica is BSD-2-Clause, and bundled
native dependencies retain their own licenses in each native package. Optional adapters retain the
licenses of ImageSharp and SkiaSharp.
