using System.Net;

namespace AzUrlShortener.Core.Services;

public class AzUrlShortenerException : Exception
{
    public HttpStatusCode StatusCode { get; set; }
    public AzUrlShortenerException(string message) : base(message)
    {
    }

    public AzUrlShortenerException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}