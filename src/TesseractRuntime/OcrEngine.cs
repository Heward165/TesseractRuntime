using System.Diagnostics;
using TesseractRuntime.Native;

namespace TesseractRuntime;

/// <summary>A safe owner for one native <c>TessBaseAPI</c> instance.</summary>
public sealed class OcrEngine : IOcrEngine
{
    // SemaphoreSlim allocates no wait handle in this usage. Disposing it would race callers that
    // were already queued when disposal began; the disposal gate rejects all later callers.
#pragma warning disable CA2213
    private readonly SemaphoreSlim gate = new(1, 1);
#pragma warning restore CA2213
    private readonly SafeTesseractHandle handle;
    private readonly ITesseractApi api;
    private readonly OcrEngineOptions options;
    private bool disposed;
    private int disposalStarted;

    private OcrEngine(SafeTesseractHandle handle, ITesseractApi api, OcrEngineOptions options, string version)
    {
        this.handle = handle;
        this.api = api;
        this.options = options;
        Version = version;
    }

    public string Version { get; }

    public static OcrEngine Create(OcrEngineOptions options)
    {
        try
        {
            return Create(options, NativeTesseractApi.Instance);
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

    internal static OcrEngine Create(OcrEngineOptions options, ITesseractApi api)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(api);

        if (!Directory.Exists(options.ResolveModelDirectory()))
        {
            throw new DirectoryNotFoundException($"Tessdata directory was not found: {options.ResolveModelDirectory()}");
        }

        var missingModels = options.FindMissingLanguageModels();
        if (missingModels.Count != 0)
        {
            throw new FileNotFoundException($"Missing traineddata models: {string.Join(", ", missingModels)}.");
        }

        var version = api.GetVersion();
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new TesseractRuntimeException("The native library returned an empty version string.");
        }

        if (!version.StartsWith('5'))
        {
            throw new TesseractRuntimeException($"TesseractRuntime requires native Tesseract 5.x; found {version}.");
        }

        var handle = SafeTesseractHandle.Create(api);
        try
        {
            var result = api.Initialize(
                handle.DangerousGetHandle(),
                options.ResolveModelDirectory(),
                options.Language,
                options.EngineMode);

            if (result != 0)
            {
                throw new TesseractRuntimeException(
                    $"Tesseract initialization failed for language '{options.Language}' in '{options.ResolveModelDirectory()}'.");
            }

            foreach (var variable in options.Variables)
            {
                if (!api.SetVariable(handle.DangerousGetHandle(), variable.Key, variable.Value))
                {
                    throw new TesseractRuntimeException($"Tesseract rejected variable '{variable.Key}'.");
                }
            }

            api.SetPageSegmentationMode(handle.DangerousGetHandle(), options.PageSegmentationMode);
            return new OcrEngine(handle, api, options, version);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public async ValueTask<OcrResult> RecognizeAsync(
        OcrImage image,
        OcrRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposalStarted) != 0, this);
        request ??= new OcrRequest();
        request.Validate();

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            return RecognizeCore(image, request, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<OcrOrientation?> DetectOrientationAsync(
        OcrImage image,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposalStarted) != 0, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            var orientation = DetectOrientationCore(image);
            cancellationToken.ThrowIfCancellationRequested();
            return orientation;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposalStarted, 1) != 0)
        {
            return;
        }

        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            handle.Dispose();
        }
        finally
        {
            gate.Release();
        }
    }

    private unsafe OcrResult RecognizeCore(OcrImage image, OcrRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var pinned = image.Pixels.Pin();
        try
        {
            // Tesseract 5.5.2's PAGE serializer hashes GetInputName(). Its empty-name fallback
            // supplies null, which can crash buffer-based OCR. Always establish a managed name.
            api.SetInputName(handle.DangerousGetHandle(), request.InputName);
            api.SetPageSegmentationMode(
                handle.DangerousGetHandle(),
                request.PageSegmentationMode ?? options.PageSegmentationMode);
            api.SetImage(
                handle.DangerousGetHandle(),
                (nint)pinned.Pointer,
                image.Width,
                image.Height,
                image.BytesPerPixel,
                image.Stride);

            if (request.SourceResolution is { } resolution)
            {
                api.SetSourceResolution(handle.DangerousGetHandle(), resolution);
            }

            if (api.Recognize(handle.DangerousGetHandle()) != 0)
            {
                throw new TesseractRuntimeException("Tesseract failed to recognize the supplied image.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var text = api.GetUtf8Text(handle.DangerousGetHandle());
            var confidence = api.MeanTextConfidence(handle.DangerousGetHandle()) / 100F;
            var formats = request.OutputFormats;

            return new OcrResult(
                text,
                confidence,
                stopwatch.Elapsed,
                formats.HasFlag(OcrOutputFormat.Hocr)
                    ? api.GetHocrText(handle.DangerousGetHandle(), request.PageNumber)
                    : null,
                formats.HasFlag(OcrOutputFormat.Tsv)
                    ? api.GetTsvText(handle.DangerousGetHandle(), request.PageNumber)
                    : null,
                formats.HasFlag(OcrOutputFormat.AltoXml)
                    ? api.GetAltoText(handle.DangerousGetHandle(), request.PageNumber)
                    : null,
                formats.HasFlag(OcrOutputFormat.PageXml)
                    ? api.GetPageText(handle.DangerousGetHandle(), request.PageNumber)
                    : null);
        }
        finally
        {
            api.Clear(handle.DangerousGetHandle());
            api.SetPageSegmentationMode(handle.DangerousGetHandle(), options.PageSegmentationMode);
        }
    }

    private unsafe OcrOrientation? DetectOrientationCore(OcrImage image)
    {
        using var pinned = image.Pixels.Pin();
        api.SetImage(
            handle.DangerousGetHandle(),
            (nint)pinned.Pointer,
            image.Width,
            image.Height,
            image.BytesPerPixel,
            image.Stride);

        try
        {
            return api.DetectOrientationScript(handle.DangerousGetHandle());
        }
        finally
        {
            api.Clear(handle.DangerousGetHandle());
        }
    }
}
