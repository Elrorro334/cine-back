namespace Cine.Application.Exceptions;

public class TwilioSmsException : Exception
{
    public int StatusCode { get; }

    public TwilioSmsException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
