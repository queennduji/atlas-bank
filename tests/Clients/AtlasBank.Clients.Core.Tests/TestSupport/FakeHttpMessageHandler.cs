using System.Net;

namespace AtlasBank.Clients.Core.Tests.TestSupport;

/// <summary>
/// A minimal stand-in for the network so ApiClient/OidcAuthenticator tests never make a real
/// HTTP call. Queue responses with <see cref="Enqueue"/> in the order the code under test is
/// expected to request them, or override <see cref="Handler"/> for request-shape assertions.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage>? Handler { get; set; }

    public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

    public void EnqueueJson(HttpStatusCode statusCode, string json) =>
        Enqueue(new HttpResponseMessage(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (Handler is not null)
        {
            return Task.FromResult(Handler(request));
        }

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"No queued response for {request.Method} {request.RequestUri}.");
        }

        return Task.FromResult(_responses.Dequeue());
    }
}
