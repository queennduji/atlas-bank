using System.Net;
using System.Net.Http.Headers;
using AtlasBank.Clients.Core.Auth;

namespace AtlasBank.Clients.Core.Http;

/// <summary>
/// Attaches the access token to every Gateway request. On a 401 (token expired mid-flight,
/// or got revoked) it refreshes once and retries before giving up, so ViewModels calling the
/// API don't have to think about token lifetime at all.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly OidcAuthenticator _authenticator;

    public BearerTokenHandler(OidcAuthenticator authenticator)
    {
        _authenticator = authenticator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await AttachTokenAsync(request, forceRefresh: false, ct).ConfigureAwait(false);
        var response = await base.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        await AttachTokenAsync(request, forceRefresh: true, ct).ConfigureAwait(false);
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }

    private async Task AttachTokenAsync(HttpRequestMessage request, bool forceRefresh, CancellationToken ct)
    {
        var token = await _authenticator.GetAccessTokenAsync(forceRefresh, ct).ConfigureAwait(false);
        request.Headers.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    }
}
