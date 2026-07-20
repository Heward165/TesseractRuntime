using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TesseractRuntime.Native;

#pragma warning disable CA5392 // LoadLibraryEx receives explicit safe-search flags at the call site.
internal static partial class TesseractNativeResolver
{
    private const uint LoadLibrarySearchDllLoadDirectory = 0x00000100;
    private const uint LoadLibrarySearchDefaultDirectories = 0x00001000;
    private static readonly object Sync = new();
    private static string? loadedPath;

    internal static string? LoadedPath
    {
        get
        {
            lock (Sync)
            {
                return loadedPath;
            }
        }
    }

#pragma warning disable CA2255 // A module initializer guarantees registration before the first generated P/Invoke stub runs.
    [ModuleInitializer]
    internal static void Initialize()
    {
        NativeLibrary.SetDllImportResolver(typeof(TesseractNativeResolver).Assembly, Resolve);
    }
#pragma warning restore CA2255

    internal static IReadOnlyList<string> GetCandidatePaths()
    {
        var names = GetLibraryNames();
        var candidates = new List<string>();
        var configuredPath = Environment.GetEnvironmentVariable("TESSERACT_RUNTIME_NATIVE_PATH");

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var expanded = Environment.ExpandEnvironmentVariables(configuredPath);
            if (Path.HasExtension(expanded))
            {
                candidates.Add(Path.GetFullPath(expanded));
            }
            else
            {
                candidates.AddRange(names.Select(name => Path.GetFullPath(Path.Combine(expanded, name))));
            }
        }

        var packageDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            RuntimeInformation.RuntimeIdentifier,
            "native");

        candidates.AddRange(names.Select(name => Path.Combine(packageDirectory, name)));
        candidates.AddRange(names.Select(name => Path.Combine(AppContext.BaseDirectory, name)));
        candidates.AddRange(names);
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, TesseractNative.LibraryName, StringComparison.Ordinal))
        {
            return 0;
        }

        lock (Sync)
        {
            foreach (var candidate in GetCandidatePaths())
            {
                nint handle;
                if (Path.IsPathFullyQualified(candidate))
                {
                    if (!File.Exists(candidate) || !TryLoadAbsolute(candidate, out handle))
                    {
                        continue;
                    }
                }
                else if (!NativeLibrary.TryLoad(candidate, assembly, searchPath, out handle))
                {
                    continue;
                }

                loadedPath = candidate;
                return handle;
            }
        }

        var attempted = string.Join(Environment.NewLine, GetCandidatePaths().Select(path => $"  - {path}"));
        throw new DllNotFoundException(
            $"Tesseract 5 native library was not found. Set TESSERACT_RUNTIME_NATIVE_PATH or install a " +
            $"TesseractRuntime.Native.<rid> package.{Environment.NewLine}Attempted:{Environment.NewLine}{attempted}");
    }

    private static bool TryLoadAbsolute(string path, out nint handle)
    {
        // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR lets Windows resolve Leptonica and codec DLLs next
        // to the selected Tesseract binary without modifying the process-wide PATH.
        if (OperatingSystem.IsWindows())
        {
            handle = LoadLibraryEx(
                path,
                0,
                LoadLibrarySearchDllLoadDirectory | LoadLibrarySearchDefaultDirectories);
            return handle != 0;
        }

        return NativeLibrary.TryLoad(path, out handle);
    }

    private static string[] GetLibraryNames()
    {
        if (OperatingSystem.IsWindows())
        {
            return ["tesseract55.dll", "libtesseract-5.dll", "libtesseract.dll", "tesseract.dll"];
        }

        if (OperatingSystem.IsMacOS())
        {
            return ["libtesseract.5.dylib", "libtesseract.dylib", "tesseract"];
        }

        return ["libtesseract.so.5", "libtesseract.so", "tesseract"];
    }

    [LibraryImport("kernel32", EntryPoint = "LoadLibraryExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint LoadLibraryEx(string fileName, nint file, uint flags);
}
#pragma warning restore CA5392
