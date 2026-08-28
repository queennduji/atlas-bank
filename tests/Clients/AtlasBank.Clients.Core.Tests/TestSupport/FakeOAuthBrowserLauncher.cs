using AtlasBank.Clients.Core.Auth;

namespace AtlasBank.Clients.Core.Tests.TestSupport;

/// <summary>Stands in for a real browser: instead of showing the user anything, it lets the
/// test decide what Keycloak "redirected back" with, optionally inspecting the authorization
/// URL OidcAuthenticator built (to assert PKCE/state parameters landed on it correctly).</summary>
public sealed class FakeOAuthBrowserLauncher : IOAuthBrowserLauncher
{
    public Uri RedirectUri { get; init; } = new("http://127.0.0.1:51739/atlasbank-callback/");

    public Uri? LastAuthorizationUri { get; private set; }

    /// <summary>Given the authorization URL that was built, returns what the "browser"
    /// redirected back with. Defaults to echoing the real `state` param back alongside a
    /// fake authorization code, simulating a successful login.</summary>
    public Func<Uri, IReadOnlyDictionary<string, string>> RespondWith { get; set; } = uri =>
    {
        var state = System.Web.HttpUtility.ParseQueryString(uri.Query)["state"] ?? string.Empty;
        return new Dictionary<string, string> { ["code"] = "fake-authorization-code", ["state"] = state };
    };

    public Task<IReadOnlyDictionary<string, string>> AuthenticateAsync(Uri authorizationUri, CancellationToken ct)
    {
        LastAuthorizationUri = authorizationUri;
        return Task.FromResult(RespondWith(authorizationUri));
    }
}
