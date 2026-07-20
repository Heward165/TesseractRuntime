using Microsoft.Win32.SafeHandles;

namespace TesseractRuntime.Native;

internal sealed class SafeTesseractHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly ITesseractApi api;

    internal SafeTesseractHandle(ITesseractApi api)
        : base(ownsHandle: true)
    {
        this.api = api;
    }

    internal static SafeTesseractHandle Create(ITesseractApi api)
    {
        var pointer = api.Create();
        if (pointer == 0)
        {
            throw new TesseractRuntimeException("Tesseract could not allocate an engine.");
        }

        var handle = new SafeTesseractHandle(api);
        handle.SetHandle(pointer);
        return handle;
    }

    protected override bool ReleaseHandle()
    {
        api.Delete(handle);
        return true;
    }
}
