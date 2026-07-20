namespace TesseractRuntime.Native;

internal interface ITesseractApi
{
    string GetVersion();

    nint Create();

    void Delete(nint handle);

    int Initialize(nint handle, string dataPath, string language, OcrEngineMode mode);

    bool SetVariable(nint handle, string name, string value);

    void SetInputName(nint handle, string inputName);

    void SetPageSegmentationMode(nint handle, OcrPageSegmentationMode mode);

    void SetImage(nint handle, nint imageData, int width, int height, int bytesPerPixel, int bytesPerLine);

    void SetSourceResolution(nint handle, int pixelsPerInch);

    int Recognize(nint handle);

    string GetUtf8Text(nint handle);

    string GetHocrText(nint handle, int pageNumber);

    string GetTsvText(nint handle, int pageNumber);

    string GetAltoText(nint handle, int pageNumber);

    string GetPageText(nint handle, int pageNumber);

    int MeanTextConfidence(nint handle);

    OcrOrientation? DetectOrientationScript(nint handle);

    void Clear(nint handle);
}
