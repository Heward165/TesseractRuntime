using TesseractRuntime.Pooling;

namespace TesseractRuntime.Tests;

public sealed class PoolLifecycleTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateRejectsNonPositiveSize(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OcrEnginePool.Create(size, () => new FakeOcrEngine()));
    }

    [Fact]
    public void CreateRejectsNullFactoryAndNullEngine()
    {
        Assert.Throws<ArgumentNullException>(() => OcrEnginePool.Create(1, null!));
        Assert.Throws<InvalidOperationException>(() => OcrEnginePool.Create(1, () => null!));
    }

    [Fact]
    public void OptionsFactoryRejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => OcrEnginePool.Create(null!, 1));
    }

    [Fact]
    public void FactoryFailureDisposesPreviouslyCreatedEngines()
    {
        var first = CreateFakeEngine();
        var calls = 0;

        Assert.Throws<InvalidOperationException>(() => OcrEnginePool.Create(2, () =>
        {
            calls++;
            return calls == 1 ? first : throw new InvalidOperationException("factory failed");
        }));

        Assert.Equal(1, first.DisposeCount);
    }

    [Fact]
    public async Task DisposeWaitsForOutstandingLeaseThenDisposesEngine()
    {
        var engine = CreateFakeEngine();
        var pool = OcrEnginePool.Create(1, () => engine);
        var lease = await pool.RentAsync(TestContext.Current.CancellationToken);

        var disposal = pool.DisposeAsync().AsTask();
        await Task.Delay(30, TestContext.Current.CancellationToken);
        Assert.False(disposal.IsCompleted);

        await lease.DisposeAsync();
        await disposal;
        Assert.Equal(1, engine.DisposeCount);
    }

    [Fact]
    public async Task DisposeCancelsPendingRenterAndIsIdempotent()
    {
        var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine());
        var lease = await pool.RentAsync(TestContext.Current.CancellationToken);
        var pending = pool.RentAsync(TestContext.Current.CancellationToken).AsTask();
        var disposal = pool.DisposeAsync().AsTask();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pending);
        await lease.DisposeAsync();
        await disposal;
        await pool.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await pool.RentAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenterCancellationDoesNotLoseEngine()
    {
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine());
        await using var lease = await pool.RentAsync(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var pending = pool.RentAsync(cancellation.Token).AsTask();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        await lease.DisposeAsync();
        await using var next = await pool.RentAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(next.Engine);
    }

    [Fact]
    public async Task DisposedLeaseRejectsEngineAccess()
    {
        await using var pool = OcrEnginePool.Create(1, () => new FakeOcrEngine());
        var lease = await pool.RentAsync(TestContext.Current.CancellationToken);
        await lease.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => lease.Engine);
    }

    private static FakeOcrEngine CreateFakeEngine() => new();
}
