using System.Net.Http.Json;

namespace BusinessMapNET.Core.Http;

/// <summary>
/// Base class for the Businessmap resource clients. Encapsulates sending requests,
/// unwrapping the standard <c>data</c> envelope and translating failures into
/// <see cref="BusinessMapApiException"/>.
/// </summary>
public abstract class BusinessMapApiClient
{
    /// <summary>
    /// The pre-configured <see cref="HttpClient"/> (base address and <c>apikey</c> header set by DI).
    /// </summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessMapApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    protected BusinessMapApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Sends a GET request and returns the unwrapped payload.
    /// </summary>
    protected Task<T> GetAsync<T>(string relativeUri, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, relativeUri, content: null, cancellationToken);

    /// <summary>
    /// Sends a GET request for a paginated endpoint. Like <see cref="GetAsync{T}"/>, the response is
    /// wrapped in the standard <see cref="ApiResponse{T}"/> envelope; here the <c>data</c> payload is
    /// itself a <see cref="PagedResult{T}"/> carrying the <c>pagination</c>/<c>data</c> pair.
    /// </summary>
    protected Task<PagedResult<T>> GetPagedAsync<T>(string relativeUri, CancellationToken cancellationToken = default) =>
        SendPagedAsync<T>(HttpMethod.Get, relativeUri, content: null, cancellationToken);

    /// <summary>
    /// Sends a POST request with a JSON body and returns the unwrapped payload.
    /// </summary>
    protected Task<T> PostAsync<T>(string relativeUri, object? body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, relativeUri, CreateJsonContent(body), cancellationToken);

    /// <summary>
    /// Sends a PATCH request with a JSON body and returns the unwrapped payload.
    /// </summary>
    protected Task<T> PatchAsync<T>(string relativeUri, object? body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Patch, relativeUri, CreateJsonContent(body), cancellationToken);

    /// <summary>
    /// Sends a DELETE request. The response body, if any, is ignored.
    /// </summary>
    protected async Task DeleteAsync(string relativeUri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, relativeUri);
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, HttpMethod.Delete, relativeUri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string relativeUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUri) { Content = content };
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, method, relativeUri, cancellationToken).ConfigureAwait(false);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiResponse<T>>(BusinessMapJson.Options, cancellationToken)
            .ConfigureAwait(false);

        if (envelope is null || envelope.Data is null)
        {
            throw new BusinessMapApiException(
                response.StatusCode,
                responseBody: "The response body did not contain the expected 'data' payload.",
                method.Method,
                relativeUri);
        }

        return envelope.Data;
    }

    private async Task<PagedResult<T>> SendPagedAsync<T>(
        HttpMethod method,
        string relativeUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUri) { Content = content };
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, method, relativeUri, cancellationToken).ConfigureAwait(false);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<T>>>(BusinessMapJson.Options, cancellationToken)
            .ConfigureAwait(false);

        if (envelope is null || envelope.Data is null)
        {
            throw new BusinessMapApiException(
                response.StatusCode,
                responseBody: "The response body did not contain the expected paginated payload.",
                method.Method,
                relativeUri);
        }

        return envelope.Data;
    }

    private static HttpContent? CreateJsonContent(object? body) =>
        body is null ? null : JsonContent.Create(body, mediaType: null, BusinessMapJson.Options);

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        HttpMethod method,
        string relativeUri,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Ignore body-read failures; the status code is still meaningful.
        }

        throw new BusinessMapApiException(response.StatusCode, body, method.Method, relativeUri);
    }
}
