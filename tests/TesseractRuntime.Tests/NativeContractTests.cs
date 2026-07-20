using System.Runtime.InteropServices;
using TesseractRuntime.Native;

namespace TesseractRuntime.Tests;

[Collection("Process environment")]
public sealed class NativeContractTests
{
    [Fact]
    public void NativeEnumValuesMatchTesseractCapi()
    {
        Assert.Equal(Enumerable.Range(0, 14), Enum.GetValues<OcrPageSegmentationMode>().Select(value => (int)value));
        Assert.Equal(Enumerable.Range(0, 4), Enum.GetValues<OcrEngineMode>().Select(value => (int)value));
        Assert.Equal(0, (int)OcrOutputFormat.None);
        Assert.Equal(1, (int)OcrOutputFormat.Hocr);
        Assert.Equal(2, (int)OcrOutputFormat.Tsv);
        Assert.Equal(4, (int)OcrOutputFormat.AltoXml);
        Assert.Equal(8, (int)OcrOutputFormat.PageXml);
    }

    [Fact]
    public void NativeBindingDeclaresEveryRequiredEntryPoint()
    {
        var expected = new[]
        {
            "TessVersion", "TessDeleteText", "TessBaseAPICreate", "TessBaseAPIDelete",
            "TessBaseAPIInit2", "TessBaseAPISetVariable", "TessBaseAPISetInputName", "TessBaseAPISetPageSegMode",
            "TessBaseAPISetImage", "TessBaseAPISetSourceResolution", "TessBaseAPIRecognize",
            "TessBaseAPIGetUTF8Text", "TessBaseAPIGetHOCRText", "TessBaseAPIGetTsvText",
            "TessBaseAPIGetAltoText", "TessBaseAPIGetPAGEText", "TessBaseAPIMeanTextConf",
            "TessBaseAPIDetectOrientationScript", "TessBaseAPIClear",
        };
        var declared = typeof(TesseractNative)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Select(method => method.GetCustomAttributes(typeof(LibraryImportAttribute), inherit: false)
                .Cast<LibraryImportAttribute>()
                .SingleOrDefault()?.EntryPoint)
            .Where(entryPoint => entryPoint is not null)
            .ToArray();

        Assert.Equal(expected.Order(), declared.Order());
    }

    [Fact]
    public void ConfiguredNativeFileIsFirstCandidate()
    {
        var previous = Environment.GetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH");
        var configured = Path.Combine(Path.GetTempPath(), "custom-tesseract.dll");
        try
        {
            Environment.SetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH", configured);

            Assert.Equal(Path.GetFullPath(configured), TesseractRuntimeInfo.NativeLibraryCandidates[0]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH", previous);
        }
    }

    [Fact]
    public void ConfiguredNativeDirectoryExpandsPlatformNamesWithoutDuplicates()
    {
        var previous = Environment.GetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH");
        try
        {
            Environment.SetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH", Path.GetTempPath());
            var candidates = TesseractRuntimeInfo.NativeLibraryCandidates;

            Assert.Equal(candidates.Count, candidates.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.True(Path.IsPathFullyQualified(candidates[0]));
            Assert.Contains(candidates, path => path.Contains("tesseract", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH", previous);
        }
    }
}

[CollectionDefinition("Process environment", DisableParallelization = true)]
#pragma warning disable CA1515 // xUnit collection definitions must be public.
public sealed class EnvironmentSerialGroup;
#pragma warning restore CA1515
