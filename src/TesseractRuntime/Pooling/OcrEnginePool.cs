using System.Threading.Channels;

namespace TesseractRuntime.Pooling;

/// <summary>
/// A bounded pool for expensive, non-thread-safe native engines. Leases provide deterministic
/// return even when recognition fails or is cancelled.
/// </summary>
public sealed class OcrEnginePool : IAsyncDisposable
{
    private readonly Channel<IOcrEngine> engines;
    private readonly CancellationTokenSource shutdown = new();
    private int disposeStarted;

    private OcrEnginePool(IReadOnlyCollection<IOcrEngine> engines)
    {
        Size = engines.Count;
        this.engines = Channel.CreateBounded<IOcrEngine>(new BoundedChannelOptions(Size)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        foreach (var engine in engines)
        {
            if (!this.engines.Writer.TryWrite(engine))
            {
                throw new InvalidOperationException("The engine pool could not be initialized.");
            }
        }
    }

    public int Size { get; }

    public static OcrEnginePool Create(OcrEngineOptions options, int size)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Create(size, () => OcrEngine.Create(options));
    }

    public static OcrEnginePool Create(int size, Func<IOcrEngine> engineFactory)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        ArgumentNullException.ThrowIfNull(engineFactory);

        var created = new List<IOcrEngine>(size);
        try
        {
            for (var index = 0; index < size; index++)
            {
                created.Add(engineFactory() ?? throw new InvalidOperationException("The engine factory returned null."));
            }

            return new OcrEnginePool(created);
        }
        catch
        {
            foreach (var engine in created)
            {
                engine.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            throw;
        }
    }

    public async ValueTask<OcrEngineLease> RentAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposeStarted) != 0, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdown.Token);

        try
        {
            var engine = await engines.Reader.ReadAsync(linked.Token).ConfigureAwait(false);
            return new OcrEngineLease(this, engine);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ObjectDisposedException(nameof(OcrEnginePool));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposeStarted, 1) != 0)
        {
            return;
        }

        await shutdown.CancelAsync().ConfigureAwait(false);

        // Reading exactly Size instances waits for outstanding leases without polling. Pending
        // renters are cancelled above, so every engine eventually returns to this reader.
        for (var index = 0; index < Size; index++)
        {
            var engine = await engines.Reader.ReadAsync().ConfigureAwait(false);
            await engine.DisposeAsync().ConfigureAwait(false);
        }

        engines.Writer.TryComplete();
        shutdown.Dispose();
    }

    internal void Return(IOcrEngine engine)
    {
        if (!engines.Writer.TryWrite(engine))
        {
            engine.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

}
