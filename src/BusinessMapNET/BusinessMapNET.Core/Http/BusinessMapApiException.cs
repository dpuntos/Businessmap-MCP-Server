using System.Net;

namespace BusinessMapNET.Core.Http;

/// <summary>
/// Exception thrown when the Businessmap API returns an unsuccessful HTTP status code.
/// </summary>
public sealed class BusinessMapApiException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The raw response body returned by the API, if any.
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// The HTTP method of the failed request.
    /// </summary>
    public string? RequestMethod { get; }

    /// <summary>
    /// The relative request URI of the failed request.
    /// </summary>
    public string? RequestUri { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapApiException"/> class.
    /// </summary>
    public BusinessMapApiException(
        HttpStatusCode statusCode,
        string? responseBody,
        string? requestMethod,
        string? requestUri)
        : base($"Businessmap API request '{requestMethod} {requestUri}' failed with status code {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        RequestMethod = requestMethod;
        RequestUri = requestUri;
    }
}
