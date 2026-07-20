using TesseractRuntime;
using TesseractRuntime.Batching;
using TesseractRuntime.Pooling;
using System.Globalization;

var checks = new (string Name, Func<Task> Run)[]
{
    ("pixel buffers are isolated", PixelBuffersAreIsolated),
    ("unsupported formats are rejected", UnsupportedFormatsAreRejected),
    ("configuration collections are isolated", ConfigurationCollectionsAreIsolated),
    ("combined language models are validated", CombinedLanguageModelsAreValidated),
    ("pool capacity is enforced", PoolCapacityIsEnforced),
    ("leases return engines once", LeasesReturnEnginesOnce),
    ("batch ordering survives partial failure", BatchOrderingSurvivesFailure),
    ("language candidates are ranked by confidence", LanguageCandidatesAreRanked),
};

var failures = 0;
foreach (var check in checks)
{
    try
    {
        await check.Run();
        Console.WriteLine($"PASS {check.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {check.Name}: {exception.Message}");
    }
}

Console.WriteLine($"{checks.Length - failures}/{checks.Length} conformance checks passed.");
return failures == 0 ? 0 : 1;

static Task PixelBuffersAreIsolated()
{
    byte[] source = [7];
    var image = OcrImage.CopyFrom(source, 1, 1, 1);
    source[0] = 9;
    Require(image.Pixels.Span[0] == 7, "caller mutation leaked into OCR memory");
    return Task.CompletedTask;
}

static Task UnsupportedFormatsAreRejected()
{
    RequireThrows<ArgumentOutOfRangeException>(() => OcrImage.CopyFrom([0, 0], 1, 1, 2));
    return Task.CompletedTask;
}

static Task ConfigurationCollectionsAreIsolated()
{
    var variables = new Dictionary<string, string> { ["key"] = "before" };
    var options = new OcrEngineOptions(".", "eng", variables: variables);
    variables["key"] = "after";
    Require(options.Variables["key"] == "before", "configuration was not copied");
    return Task.CompletedTask;
}

static Task CombinedLanguageModelsAreValidated()
{
    var path = Directory.CreateTempSubdirectory();
    try
    {
        File.WriteAllBytes(Path.Combine(path.FullName, "eng.traineddata"), []);
        var missing = new OcrEngineOptions(path.FullName, "eng+por").FindMissingLanguageModels();
        Require(missing.SequenceEqual(["por"]), "missing models were reported incorrectly");
    }
    finally
    {
        path.Delete(recursive: true);
    }

    return Task.CompletedTask;
}

static async Task PoolCapacityIsEnforced()
{
    await using var pool = OcrEnginePool.Create(1, () => new StubEngine(0.8F));
    await using var first = await pool.RentAsync();
    var second = pool.RentAsync().AsTask();
    await Task.Delay(30);
    Require(!second.IsCompleted, "pool issued more engines than its capacity");
    await first.DisposeAsync();
    await using var returned = await second;
}

static async Task LeasesReturnEnginesOnce()
{
    await using var pool = OcrEnginePool.Create(1, () => new StubEngine(0.8F));
    var lease = await pool.RentAsync();
    await lease.DisposeAsync();
    await lease.DisposeAsync();
    await using var next = await pool.RentAsync();
}

static async Task BatchOrderingSurvivesFailure()
{
    await using var pool = OcrEnginePool.Create(2, () => new StubEngine(0.8F, failOn: 2));
    var batch = new OcrBatchProcessor(pool);
    var items = Enumerable.Range(1, 3)
        .Select(value => new OcrBatchItem(value.ToString(CultureInfo.InvariantCulture), OcrImage.CopyFrom([(byte)value], 1, 1, 1)));
    var results = await batch.ProcessAsync(items);
    Require(results.Select(result => result.Id).SequenceEqual(["1", "2", "3"]), "batch order changed");
    Require(!results[1].IsSuccess && results[0].IsSuccess && results[2].IsSuccess, "partial failure was not isolated");
}

static async Task LanguageCandidatesAreRanked()
{
    await using var low = OcrEnginePool.Create(1, () => new StubEngine(0.5F));
    await using var high = OcrEnginePool.Create(1, () => new StubEngine(0.9F));
    var result = await OcrLanguageDetector.DetectAsync(
        OcrImage.CopyFrom([1], 1, 1, 1),
        new Dictionary<string, OcrEnginePool> { ["low"] = low, ["high"] = high });
    Require(result.Language == "high", "highest-confidence model was not selected");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void RequireThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

sealed class StubEngine(float confidence, int? failOn = null) : IOcrEngine
{
    public string Version => "stub";

    public ValueTask<OcrResult> RecognizeAsync(
        OcrImage image,
        OcrRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var value = image.Pixels.Span[0];
        if (value == failOn)
        {
            throw new InvalidDataException("Synthetic failure.");
        }

        return ValueTask.FromResult(new OcrResult(value.ToString(CultureInfo.InvariantCulture), confidence, TimeSpan.Zero));
    }

    public ValueTask<OcrOrientation?> DetectOrientationAsync(
        OcrImage image,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<OcrOrientation?>(null);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
