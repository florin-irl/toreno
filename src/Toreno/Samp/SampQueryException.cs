namespace Toreno.Samp;

public sealed class SampQueryException : Exception
{
    public SampQueryException(string message) : base(message)
    {
    }

    public SampQueryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
