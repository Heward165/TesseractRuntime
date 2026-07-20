namespace TesseractRuntime;

/// <summary>Orientation and script estimates returned by Tesseract OSD.</summary>
public sealed record OcrOrientation(
    int ClockwiseRotationDegrees,
    float OrientationConfidence,
    string Script,
    float ScriptConfidence);
