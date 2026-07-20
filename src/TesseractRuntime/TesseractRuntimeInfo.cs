using TesseractRuntime.Native;

namespace TesseractRuntime;

/// <summary>Diagnostic information about native resolution and ABI compatibility.</summary>
public static class TesseractRuntimeInfo
{
    public static string? LoadedNativePath => TesseractNativeResolver.LoadedPath;

    public static IReadOnlyList<string> NativeLibraryCandidates => TesseractNativeResolver.GetCandidatePaths();

    public static string GetNativeVersion()
    {
        try
        {
            var version = NativeTesseractApi.Instance.GetVersion();
            return string.IsNullOrWhiteSpace(version)
                ? throw new TesseractRuntimeException("The native library returned an empty version string.")
                : version;
        }
        catch (TesseractRuntimeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            throw new TesseractRuntimeException("The Tesseract 5 native runtime could not be loaded.", exception);
        }
    }
}
