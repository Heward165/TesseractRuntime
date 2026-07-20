using TesseractRuntime;

namespace TesseractRuntime.Tests;

public sealed class OcrEngineOptionsTests
{
    [Fact]
    public void ConstructorDefensivelyCopiesVariables()
    {
        var variables = new Dictionary<string, string> { ["preserve_interword_spaces"] = "1" };
        var options = new OcrEngineOptions(".", "eng", variables: variables);

        variables["preserve_interword_spaces"] = "0";
        variables["new"] = "value";

        Assert.Equal("1", options.Variables["preserve_interword_spaces"]);
        Assert.False(options.Variables.ContainsKey("new"));
    }

    [Fact]
    public void FindMissingLanguageModelsHandlesCombinedLanguages()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllBytes(Path.Combine(directory.FullName, "eng.traineddata"), []);
            var options = new OcrEngineOptions(directory.FullName, "eng+por");

            Assert.Equal(["por"], options.FindMissingLanguageModels());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void ConstructorRejectsEmptyLanguage()
    {
        Assert.Throws<ArgumentException>(() => new OcrEngineOptions(".", " "));
    }

    [Fact]
    public void ConstructorTrimsLanguageAndNormalizesPath()
    {
        var options = new OcrEngineOptions(".", " eng ");

        Assert.Equal("eng", options.Language);
        Assert.True(Path.IsPathFullyQualified(options.DataPath));
    }

    [Fact]
    public void FindMissingModelsUsesNestedTessdataDirectory()
    {
        var parent = Directory.CreateTempSubdirectory();
        try
        {
            var nested = Directory.CreateDirectory(Path.Combine(parent.FullName, "tessdata"));
            File.WriteAllBytes(Path.Combine(nested.FullName, "eng.traineddata"), []);

            Assert.Empty(new OcrEngineOptions(parent.FullName, "eng").FindMissingLanguageModels());
        }
        finally
        {
            parent.Delete(recursive: true);
        }
    }

    [Fact]
    public void ConstructorRejectsInvalidVariables()
    {
        Assert.Throws<ArgumentException>(() =>
            new OcrEngineOptions(".", "eng", variables: new Dictionary<string, string> { [" "] = "value" }));
        Assert.Throws<ArgumentException>(() =>
            new OcrEngineOptions(".", "eng", variables: new Dictionary<string, string> { ["key"] = null! }));
    }

    [Fact]
    public void RequestRejectsNegativePageNumber()
    {
        var request = new OcrRequest { PageNumber = -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
    }

    [Fact]
    public void RequestRejectsEmptyInputName()
    {
        Assert.Throws<ArgumentException>(() => new OcrRequest { InputName = " " }.Validate());
    }

    [Fact]
    public void ExceptionConstructorsPreserveMessageAndCause()
    {
        var cause = new InvalidOperationException("cause");

        Assert.Null(new TesseractRuntimeException().InnerException);
        Assert.Equal("message", new TesseractRuntimeException("message").Message);
        Assert.Same(cause, new TesseractRuntimeException("message", cause).InnerException);
    }

    [Fact]
    public async Task RequestRejectsInvalidResolution()
    {
        var request = new OcrRequest { SourceResolution = 0 };
        var image = OcrImage.CopyFrom([0], 1, 1, 1);
        await using var engine = new FakeOcrEngine();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await engine.ValidateRequestAsync(image, request));
    }
}
