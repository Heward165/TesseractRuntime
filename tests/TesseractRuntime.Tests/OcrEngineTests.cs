using TesseractRuntime.Native;

namespace TesseractRuntime.Tests;

public sealed class OcrEngineTests
{
    [Fact]
    public async Task CreateInitializesEveryOptionAndDisposesOnce()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();
        var options = new OcrEngineOptions(
            models.Path,
            "eng",
            OcrEngineMode.Default,
            OcrPageSegmentationMode.SingleBlock,
            new Dictionary<string, string> { ["preserve_interword_spaces"] = "1" });

        var engine = OcrEngine.Create(options, api);

        Assert.Equal("5.5.2", engine.Version);
        Assert.Equal((models.Path, "eng", OcrEngineMode.Default), api.Initialization);
        Assert.Equal("1", api.Variables["preserve_interword_spaces"]);
        Assert.Equal(OcrPageSegmentationMode.SingleBlock, api.SegmentationModes.Single());

        await engine.DisposeAsync();
        await engine.DisposeAsync();
        Assert.Equal(1, api.DeleteCallCount);
    }

    [Fact]
    public void CreateRejectsMissingDirectoryBeforeLoadingNativeRuntime()
    {
        var api = new FakeTesseractApi();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => OcrEngine.Create(new OcrEngineOptions(missing, "eng"), api));
        Assert.Equal(0, api.VersionCallCount);
    }

    [Fact]
    public void CreateReportsAllMissingModelsBeforeLoadingNativeRuntime()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();

        var error = Assert.Throws<FileNotFoundException>(() =>
            OcrEngine.Create(new OcrEngineOptions(models.Path, "eng+por+spa"), api));

        Assert.Contains("por", error.Message, StringComparison.Ordinal);
        Assert.Contains("spa", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, api.VersionCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("4.1.3")]
    [InlineData("6.0.0")]
    public void CreateRejectsEmptyOrIncompatibleNativeVersion(string version)
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi { Version = version };

        Assert.Throws<TesseractRuntimeException>(() => OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api));
        Assert.Equal(0, api.CreateCallCount);
    }

    [Fact]
    public void CreateRejectsNullNativeHandle()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi { CreatedHandle = 0 };

        Assert.Throws<TesseractRuntimeException>(() => OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api));
        Assert.Equal(0, api.DeleteCallCount);
    }

    [Fact]
    public void InitializationFailureReleasesNativeHandle()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi { InitializeResult = 1 };

        Assert.Throws<TesseractRuntimeException>(() => OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api));
        Assert.Equal(1, api.DeleteCallCount);
    }

    [Fact]
    public void RejectedVariableReleasesNativeHandle()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi { RejectedVariable = "bad" };
        var options = new OcrEngineOptions(models.Path, "eng", variables: new Dictionary<string, string> { ["bad"] = "1" });

        var error = Assert.Throws<TesseractRuntimeException>(() => OcrEngine.Create(options, api));

        Assert.Contains("bad", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, api.DeleteCallCount);
    }

    [Fact]
    public async Task RecognizeMapsPixelsRequestOutputsAndConfidence()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();
        await using var engine = OcrEngine.Create(
            new OcrEngineOptions(models.Path, "eng", pageSegmentationMode: OcrPageSegmentationMode.Automatic),
            api);
        var image = OcrImage.CopyFrom(new byte[36], 3, 2, 4, stride: 18);
        var request = new OcrRequest
        {
            SourceResolution = 600,
            InputName = "scan-007.tif",
            PageNumber = 7,
            PageSegmentationMode = OcrPageSegmentationMode.SparseText,
            OutputFormats = OcrOutputFormat.Hocr | OcrOutputFormat.Tsv | OcrOutputFormat.AltoXml | OcrOutputFormat.PageXml,
        };

        var result = await engine.RecognizeAsync(image, request, TestContext.Current.CancellationToken);

        Assert.Equal("recognized", result.Text);
        Assert.Equal(0.87F, result.MeanConfidence);
        Assert.Equal(api.Hocr, result.Hocr);
        Assert.Equal(api.Tsv, result.Tsv);
        Assert.Equal(api.Alto, result.AltoXml);
        Assert.Equal(api.Page, result.PageXml);
        Assert.Equal((3, 2, 4, 18, true), api.Image);
        Assert.Equal(600, api.Resolution);
        Assert.Equal("scan-007.tif", api.InputName);
        Assert.Equal(1, api.ClearCallCount);
        Assert.Equal(
            [OcrPageSegmentationMode.Automatic, OcrPageSegmentationMode.SparseText, OcrPageSegmentationMode.Automatic],
            api.SegmentationModes);
    }

    [Fact]
    public async Task RecognizeSkipsUnrequestedStructuredOutputs()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();
        await using var engine = OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api);

        var result = await engine.RecognizeAsync(Pixel(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.Hocr);
        Assert.Null(result.Tsv);
        Assert.Null(result.AltoXml);
        Assert.Null(result.PageXml);
        Assert.Equal(0, api.HocrCallCount + api.TsvCallCount + api.AltoCallCount + api.PageCallCount);
    }

    [Fact]
    public async Task RecognitionFailureStillClearsImageAndRestoresSegmentation()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi { RecognizeResult = 1 };
        await using var engine = OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api);

        await Assert.ThrowsAsync<TesseractRuntimeException>(async () =>
            await engine.RecognizeAsync(Pixel(), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, api.ClearCallCount);
        Assert.Equal(OcrPageSegmentationMode.Automatic, api.SegmentationModes[^1]);
        Assert.Equal(0, api.Utf8CallCount);
    }

    [Fact]
    public async Task OrientationSuccessAndFailureBothClearImage()
    {
        using var models = new ModelDirectory("osd");
        var api = new FakeTesseractApi();
        await using var engine = OcrEngine.Create(new OcrEngineOptions(models.Path, "osd"), api);

        var detected = await engine.DetectOrientationAsync(Pixel(), TestContext.Current.CancellationToken);
        api.Orientation = null;
        var missing = await engine.DetectOrientationAsync(Pixel(), TestContext.Current.CancellationToken);

        Assert.Equal(new OcrOrientation(90, 12.5F, "Latin", 8.5F), detected);
        Assert.Null(missing);
        Assert.Equal(2, api.ClearCallCount);
    }

    [Fact]
    public async Task PreCancelledCallsNeverEnterNativeApi()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();
        await using var engine = OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await engine.RecognizeAsync(Pixel(), cancellationToken: cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await engine.DetectOrientationAsync(Pixel(), cancellation.Token));

        Assert.Equal(0, api.RecognizeCallCount);
        Assert.Equal(0, api.ClearCallCount);
    }

    [Fact]
    public async Task InvalidRequestIsRejectedBeforeNativeRecognition()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();
        await using var engine = OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await engine.RecognizeAsync(
                Pixel(),
                new OcrRequest { SourceResolution = 0 },
                TestContext.Current.CancellationToken));

        Assert.Equal(0, api.RecognizeCallCount);
    }

    [Fact]
    public async Task DisposedEngineRejectsEveryOperation()
    {
        using var models = new ModelDirectory("eng");
        var api = new FakeTesseractApi();
        var engine = OcrEngine.Create(new OcrEngineOptions(models.Path, "eng"), api);
        await engine.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await engine.RecognizeAsync(Pixel(), cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await engine.DetectOrientationAsync(Pixel(), TestContext.Current.CancellationToken));
    }

    private static OcrImage Pixel() => OcrImage.CopyFrom([255], 1, 1, 1);
}

internal sealed class ModelDirectory : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory();

    internal ModelDirectory(params string[] languages)
    {
        foreach (var language in languages)
        {
            File.WriteAllBytes(System.IO.Path.Combine(directory.FullName, $"{language}.traineddata"), [1]);
        }
    }

    internal string Path => directory.FullName;

    public void Dispose() => directory.Delete(recursive: true);
}
