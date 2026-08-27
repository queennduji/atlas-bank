using System.Net;

namespace AtlasBank.Clients.Core.Api;

/// <summary>Thrown for any non-2xx response from the Gateway. Mirrors ApiError in frontend/src/api/client.ts.</summary>
public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }

    public ApiException(string message, HttpStatusCode statusCode, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
