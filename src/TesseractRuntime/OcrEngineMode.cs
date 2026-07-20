namespace TesseractRuntime;

/// <summary>Selects the recognition engine compiled into Tesseract.</summary>
public enum OcrEngineMode
{
    LegacyOnly = 0,
    LstmOnly = 1,
    LegacyAndLstm = 2,
    Default = 3,
}
