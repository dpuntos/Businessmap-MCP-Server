using System.Net;
using System.Text;

namespace BusinessMapNET.Core.Tests.TestHelpers;

/// <summary>
/// A test double for <see cref="HttpMessageHandler"/> that records the last request it received
/// and returns a caller-provided response. Used to exercise the resource clients without hitting
/// the network.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    /// <summary>The last request received by the handler, if any.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>The body of the last request, read as a string, if any.</summary>
    public string? LastRequestBody { get; private set; }

    /// <summary>Initializes the handler with a fixed response.</summary>
    public StubHttpMessageHandler(HttpResponseMessage response)
        : this(_ => response)
    {
    }

    /// <summary>Initializes the handler with a per-request responder.</summary>
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        return _responder(request);
    }

    /// <summary>Creates a handler that returns the given status code and JSON body.</summary>
    public static StubHttpMessageHandler Json(HttpStatusCode statusCode, string json) =>
        new(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    /// <summary>Creates a handler that returns the given status code with no body.</summary>
    public static StubHttpMessageHandler Status(HttpStatusCode statusCode) =>
        new(new HttpResponseMessage(statusCode));

    /// <summary>Creates an <see cref="HttpClient"/> wired to this handler with a test base address.</summary>
    public HttpClient CreateClient() =>
        new(this)
        {
            BaseAddress = new Uri("https://test.kanbanize.com/api/v2/", UriKind.Absolute),
        };
}
