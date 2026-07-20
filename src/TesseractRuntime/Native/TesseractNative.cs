using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Every call is routed through the resolver below; analyzer CA5392 cannot see that the resolver
// accepts only an explicit package path or the platform's safe library search.
#pragma warning disable CA5392

namespace TesseractRuntime.Native;

/// <summary>
/// Minimal bindings to Tesseract's stable C API. Keeping the surface small makes ABI review
/// practical and avoids coupling the managed package to C++ object layouts.
/// </summary>
internal static partial class TesseractNative
{
    internal const string LibraryName = "TesseractRuntimeNative";

    [LibraryImport(LibraryName, EntryPoint = "TessVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint Version();

    [LibraryImport(LibraryName, EntryPoint = "TessDeleteText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void DeleteText(nint text);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPICreate")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint Create();

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIDelete")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void Delete(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIInit2", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int Initialize(nint handle, string dataPath, string language, OcrEngineMode mode);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPISetVariable", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int SetVariable(nint handle, string name, string value);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPISetInputName", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SetInputName(nint handle, string inputName);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPISetPageSegMode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SetPageSegmentationMode(nint handle, OcrPageSegmentationMode mode);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPISetImage")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SetImage(
        nint handle,
        nint imageData,
        int width,
        int height,
        int bytesPerPixel,
        int bytesPerLine);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPISetSourceResolution")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SetSourceResolution(nint handle, int pixelsPerInch);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIRecognize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int Recognize(nint handle, nint monitor);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIGetUTF8Text")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint GetUtf8Text(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIGetHOCRText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint GetHocrText(nint handle, int pageNumber);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIGetTsvText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint GetTsvText(nint handle, int pageNumber);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIGetAltoText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint GetAltoText(nint handle, int pageNumber);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIGetPAGEText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint GetPageText(nint handle, int pageNumber);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIMeanTextConf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int MeanTextConfidence(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIDetectOrientationScript")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int DetectOrientationScript(
        nint handle,
        out int orientationDegrees,
        out float orientationConfidence,
        out nint scriptName,
        out float scriptConfidence);

    [LibraryImport(LibraryName, EntryPoint = "TessBaseAPIClear")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void Clear(nint handle);
}
#pragma warning restore CA5392
