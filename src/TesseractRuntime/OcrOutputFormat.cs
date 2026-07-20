namespace TesseractRuntime;

/// <summary>Optional structured representations produced by one recognition pass.</summary>
[Flags]
public enum OcrOutputFormat
{
    None = 0,
    Hocr = 1,
    Tsv = 2,
    AltoXml = 4,
    PageXml = 8,
}
