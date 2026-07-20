namespace TesseractRuntime;

/// <summary>Represents a native loading, initialization, or recognition failure.</summary>
public sealed class TesseractRuntimeException : Exception
{
    public TesseractRuntimeException()
    {
    }

    public TesseractRuntimeException(string message)
        : base(message)
    {
    }

    public TesseractRuntimeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
