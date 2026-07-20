using System.Runtime.InteropServices;

namespace TesseractRuntime.Native;

internal sealed class NativeTesseractApi : ITesseractApi
{
    internal static NativeTesseractApi Instance { get; } = new();

    private NativeTesseractApi()
    {
    }

    public string GetVersion() => Marshal.PtrToStringUTF8(TesseractNative.Version()) ?? string.Empty;

    public nint Create() => TesseractNative.Create();

    public void Delete(nint handle) => TesseractNative.Delete(handle);

    public int Initialize(nint handle, string dataPath, string language, OcrEngineMode mode) =>
        TesseractNative.Initialize(handle, dataPath, language, mode);

    public bool SetVariable(nint handle, string name, string value) =>
        TesseractNative.SetVariable(handle, name, value) != 0;

    public void SetInputName(nint handle, string inputName) => TesseractNative.SetInputName(handle, inputName);

    public void SetPageSegmentationMode(nint handle, OcrPageSegmentationMode mode) =>
        TesseractNative.SetPageSegmentationMode(handle, mode);

    public void SetImage(nint handle, nint imageData, int width, int height, int bytesPerPixel, int bytesPerLine) =>
        TesseractNative.SetImage(handle, imageData, width, height, bytesPerPixel, bytesPerLine);

    public void SetSourceResolution(nint handle, int pixelsPerInch) =>
        TesseractNative.SetSourceResolution(handle, pixelsPerInch);

    public int Recognize(nint handle) => TesseractNative.Recognize(handle, 0);

    public string GetUtf8Text(nint handle) => ReadOwnedText(TesseractNative.GetUtf8Text(handle));

    public string GetHocrText(nint handle, int pageNumber) => ReadOwnedText(TesseractNative.GetHocrText(handle, pageNumber));

    public string GetTsvText(nint handle, int pageNumber) => ReadOwnedText(TesseractNative.GetTsvText(handle, pageNumber));

    public string GetAltoText(nint handle, int pageNumber) => ReadOwnedText(TesseractNative.GetAltoText(handle, pageNumber));

    public string GetPageText(nint handle, int pageNumber) => ReadOwnedText(TesseractNative.GetPageText(handle, pageNumber));

    public int MeanTextConfidence(nint handle) => TesseractNative.MeanTextConfidence(handle);

    public OcrOrientation? DetectOrientationScript(nint handle)
    {
        var detected = TesseractNative.DetectOrientationScript(
            handle,
            out var degrees,
            out var orientationConfidence,
            out var scriptPointer,
            out var scriptConfidence);

        // Tesseract returns a borrowed pointer into its internal script table here.
        // Unlike GetUTF8Text and the document-output functions, this value must not
        // be passed to TessDeleteText. Copy it while the engine is still alive.
        return detected == 0 || scriptPointer == 0
            ? null
            : new OcrOrientation(
                degrees,
                orientationConfidence,
                Marshal.PtrToStringUTF8(scriptPointer) ?? string.Empty,
                scriptConfidence);
    }

    public void Clear(nint handle) => TesseractNative.Clear(handle);

    private static string ReadOwnedText(nint pointer)
    {
        if (pointer == 0)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
        }
        finally
        {
            TesseractNative.DeleteText(pointer);
        }
    }
}
