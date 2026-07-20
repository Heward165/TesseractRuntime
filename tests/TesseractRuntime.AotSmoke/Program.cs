using TesseractRuntime;

var image = OcrImage.CopyFrom([0, 255], width: 2, height: 1, bytesPerPixel: 1);
if (image.Pixels.Length != 2 || TesseractRuntimeInfo.NativeLibraryCandidates.Count == 0)
{
    return 1;
}

Console.WriteLine("Native AOT smoke passed.");
return 0;
