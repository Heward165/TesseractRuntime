namespace TesseractRuntime;

/// <summary>Controls how Tesseract divides an image into text regions.</summary>
public enum OcrPageSegmentationMode
{
    OrientationAndScriptOnly = 0,
    AutomaticWithOrientation = 1,
    AutomaticOnly = 2,
    Automatic = 3,
    SingleColumn = 4,
    SingleVerticalBlock = 5,
    SingleBlock = 6,
    SingleLine = 7,
    SingleWord = 8,
    CircleWord = 9,
    SingleCharacter = 10,
    SparseText = 11,
    SparseTextWithOrientation = 12,
    RawLine = 13,
}
