using System.Collections.ObjectModel;

namespace TesseractRuntime;

/// <summary>Immutable initialization settings for one native Tesseract engine.</summary>
public sealed class OcrEngineOptions
{
    private readonly ReadOnlyDictionary<string, string> variables;

    public OcrEngineOptions(
        string dataPath,
        string language,
        OcrEngineMode engineMode = OcrEngineMode.LstmOnly,
        OcrPageSegmentationMode pageSegmentationMode = OcrPageSegmentationMode.Automatic,
        IReadOnlyDictionary<string, string>? variables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        DataPath = Path.GetFullPath(dataPath);
        Language = language.Trim();
        EngineMode = engineMode;
        PageSegmentationMode = pageSegmentationMode;

        var copy = variables is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(variables, StringComparer.Ordinal);

        if (copy.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
        {
            throw new ArgumentException("Variable names must be non-empty and values cannot be null.", nameof(variables));
        }

        this.variables = new ReadOnlyDictionary<string, string>(copy);
    }

    public string DataPath { get; }

    public string Language { get; }

    public OcrEngineMode EngineMode { get; }

    public OcrPageSegmentationMode PageSegmentationMode { get; }

    public IReadOnlyDictionary<string, string> Variables => variables;

    /// <summary>Checks model files before entering native code and returns every missing language.</summary>
    public IReadOnlyList<string> FindMissingLanguageModels()
    {
        var modelDirectory = ResolveModelDirectory();
        return Language.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(language => !File.Exists(Path.Combine(modelDirectory, $"{language}.traineddata")))
            .ToArray();
    }

    internal string ResolveModelDirectory()
    {
        var nested = Path.Combine(DataPath, "tessdata");
        return Directory.Exists(nested) ? nested : DataPath;
    }
}
