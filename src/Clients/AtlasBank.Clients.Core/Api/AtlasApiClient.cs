using AtlasBank.Clients.Core.Json;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AtlasBank.Clients.Core.Api;

/// <summary>
/// Typed client for the AtlasBank API Gateway — this file has the shared HTTP plumbing
/// (auth header, JSON, error mapping); the actual endpoint calls live in the sibling
/// partial files split by resource (AtlasApiClient.Accounts.cs, .Cards.cs, etc.).
/// </summary>
public sealed partial class AtlasApiClient
{
    private readonly HttpClient _http;

    public AtlasApiClient(HttpClient http)
    {
        _http = http;
    }

    private Task<T> GetAsync<T>(string path, CancellationToken ct) =>
        SendAsync<T>(HttpMethod.Get, path, body: null, idempotencyKey: null, ct);

    private Task<T> PostAsync<T>(string path, object? body, CancellationToken ct, string? idempotencyKey = null) =>
        SendAsync<T>(HttpMethod.Post, path, body, idempotencyKey, ct);

    private Task<T> PutAsync<T>(string path, object body, CancellationToken ct) =>
        SendAsync<T>(HttpMethod.Put, path, body, idempotencyKey: null, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, string? idempotencyKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: AtlasJsonOptions.Default);
        }
        if (idempotencyKey is not null)
        {
            // Deposit/withdraw/transfer carry an Idempotency-Key so the gateway can return the
            // original result for a retried request (a transient network drop, a user tapping
            // twice) instead of moving the money a second time — same contract the web client
            // uses (frontend/src/api/transactions.ts).
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new ApiException(ExtractErrorMessage(response.StatusCode, errorBody), response.StatusCode, errorBody);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default!;
        }

        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        if (stream.Length == 0)
        {
            return default!;
        }

        return (await JsonSerializer.DeserializeAsync<T>(stream, AtlasJsonOptions.Default, ct).ConfigureAwait(false))!;
    }

    /// <summary>Ported from extractErrorMessage in frontend/src/api/client.ts, so the same
    /// response body produces the same message on every client.</summary>
    private static string ExtractErrorMessage(HttpStatusCode status, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request failed ({(int)status})";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString() ?? body;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString()!;
                }

                if (root.TryGetProperty("title", out var title))
                {
                    // ASP.NET ValidationProblem shape: { title, errors: { field: [messages] } }
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                            {
                                var first = field.Value[0];
                                if (first.ValueKind == JsonValueKind.String)
                                {
                                    return first.GetString()!;
                                }
                            }
                        }
                    }

                    return title.GetString() ?? body;
                }
            }

            return body;
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
