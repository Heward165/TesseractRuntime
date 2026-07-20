using TesseractRuntime.Native;

namespace TesseractRuntime.Tests;

internal sealed class FakeTesseractApi : ITesseractApi
{
    internal string Version { get; set; } = "5.5.2";

    internal nint CreatedHandle { get; set; } = 42;

    internal int InitializeResult { get; set; }

    internal string? RejectedVariable { get; set; }

    internal int RecognizeResult { get; set; }

    internal string Text { get; set; } = "recognized";

    internal string Hocr { get; set; } = "<div>hocr</div>";

    internal string Tsv { get; set; } = "tsv";

    internal string Alto { get; set; } = "<alto/>";

    internal string Page { get; set; } = "<PcGts/>";

    internal int Confidence { get; set; } = 87;

    internal OcrOrientation? Orientation { get; set; } = new(90, 12.5F, "Latin", 8.5F);

    internal int VersionCallCount { get; private set; }

    internal int CreateCallCount { get; private set; }

    internal int DeleteCallCount { get; private set; }

    internal int ClearCallCount { get; private set; }

    internal int RecognizeCallCount { get; private set; }

    internal int Utf8CallCount { get; private set; }

    internal int HocrCallCount { get; private set; }

    internal int TsvCallCount { get; private set; }

    internal int AltoCallCount { get; private set; }

    internal int PageCallCount { get; private set; }

    internal int? Resolution { get; private set; }

    internal string? InputName { get; private set; }

    internal (int Width, int Height, int BytesPerPixel, int Stride, bool HasPointer)? Image { get; private set; }

    internal List<OcrPageSegmentationMode> SegmentationModes { get; } = [];

    internal Dictionary<string, string> Variables { get; } = new(StringComparer.Ordinal);

    internal (string DataPath, string Language, OcrEngineMode Mode)? Initialization { get; private set; }

    public string GetVersion()
    {
        VersionCallCount++;
        return Version;
    }

    public nint Create()
    {
        CreateCallCount++;
        return CreatedHandle;
    }

    public void Delete(nint handle)
    {
        DeleteCallCount++;
    }

    public int Initialize(nint handle, string dataPath, string language, OcrEngineMode mode)
    {
        Initialization = (dataPath, language, mode);
        return InitializeResult;
    }

    public bool SetVariable(nint handle, string name, string value)
    {
        Variables.Add(name, value);
        return !string.Equals(name, RejectedVariable, StringComparison.Ordinal);
    }

    public void SetInputName(nint handle, string inputName) => InputName = inputName;

    public void SetPageSegmentationMode(nint handle, OcrPageSegmentationMode mode) => SegmentationModes.Add(mode);

    public void SetImage(nint handle, nint imageData, int width, int height, int bytesPerPixel, int bytesPerLine)
    {
        Image = (width, height, bytesPerPixel, bytesPerLine, imageData != 0);
    }

    public void SetSourceResolution(nint handle, int pixelsPerInch) => Resolution = pixelsPerInch;

    public int Recognize(nint handle)
    {
        RecognizeCallCount++;
        return RecognizeResult;
    }

    public string GetUtf8Text(nint handle)
    {
        Utf8CallCount++;
        return Text;
    }

    public string GetHocrText(nint handle, int pageNumber)
    {
        HocrCallCount++;
        return Hocr;
    }

    public string GetTsvText(nint handle, int pageNumber)
    {
        TsvCallCount++;
        return Tsv;
    }

    public string GetAltoText(nint handle, int pageNumber)
    {
        AltoCallCount++;
        return Alto;
    }

    public string GetPageText(nint handle, int pageNumber)
    {
        PageCallCount++;
        return Page;
    }

    public int MeanTextConfidence(nint handle) => Confidence;

    public OcrOrientation? DetectOrientationScript(nint handle) => Orientation;

    public void Clear(nint handle) => ClearCallCount++;
}
