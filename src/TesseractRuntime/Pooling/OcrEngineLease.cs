namespace TesseractRuntime.Pooling;

/// <summary>Returns one engine to its owning pool when disposed.</summary>
public sealed class OcrEngineLease : IAsyncDisposable
{
    private OcrEnginePool? owner;
    private IOcrEngine? engine;

    internal OcrEngineLease(OcrEnginePool owner, IOcrEngine engine)
    {
        this.owner = owner;
        this.engine = engine;
    }

    public IOcrEngine Engine => engine ?? throw new ObjectDisposedException(nameof(OcrEngineLease));

    public ValueTask DisposeAsync()
    {
        var returnedEngine = Interlocked.Exchange(ref engine, null);
        var returnedOwner = Interlocked.Exchange(ref owner, null);
        if (returnedEngine is not null && returnedOwner is not null)
        {
            returnedOwner.Return(returnedEngine);
        }

        return ValueTask.CompletedTask;
    }
}
