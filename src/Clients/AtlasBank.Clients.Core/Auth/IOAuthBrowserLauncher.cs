namespace AtlasBank.Clients.Core.Auth;

/// <summary>
/// Shows the user the login page and hands back whatever Keycloak appended to the redirect
/// (<c>code</c> + <c>state</c> on success, <c>error</c>/<c>error_description</c> if they
/// cancelled). Two implementations because "pop a browser and catch the redirect" works
/// differently per platform: mobile uses a custom URL scheme + WebAuthenticator (see
/// AtlasBank.Maui's MobileOAuthBrowserLauncher), desktop has no OS redirect hook so
/// <see cref="LoopbackOAuthBrowserLauncher"/> opens the system browser and catches it on a
/// local HTTP listener instead – same trick the Azure CLI and gcloud use.
/// </summary>
public interface IOAuthBrowserLauncher
{
    /// <summary>redirect_uri for the authorization request. Fixed on mobile; on desktop this
    /// isn't known until the listener has actually bound a port.</summary>
    Uri RedirectUri { get; }

    /// <summary>Opens <paramref name="authorizationUri"/> and waits for the redirect back to
    /// <see cref="RedirectUri"/>, returning its query params as-is.</summary>
    Task<IReadOnlyDictionary<string, string>> AuthenticateAsync(Uri authorizationUri, CancellationToken ct);
}
