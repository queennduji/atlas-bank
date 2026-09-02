#if ANDROID || IOS || MACCATALYST
using AtlasBank.Clients.Core.Auth;
using AtlasBank.Maui.Config;

namespace AtlasBank.Maui.Services.Auth;

/// <summary>
/// Mobile side of <see cref="IOAuthBrowserLauncher"/>. Hands the authorization URL to
/// <see cref="WebAuthenticator"/>, which opens an OS-managed browser tab (Chrome Custom Tabs
/// on Android, ASWebAuthenticationSession on iOS/MacCatalyst) instead of an in-app WebView –
/// this app never sees the Keycloak password, and an existing browser session means the user
/// might not even have to type it. The redirect comes back via the "atlasbank://" scheme
/// registered per platform (Platforms/Android/OAuthCallbackActivity.cs, the iOS/MacCatalyst
/// Info.plist entries).
/// </summary>
public sealed class MobileOAuthBrowserLauncher : IOAuthBrowserLauncher
{
    public Uri RedirectUri { get; } = new($"{AppConfig.MobileRedirectScheme}://callback");

    public async Task<IReadOnlyDictionary<string, string>> AuthenticateAsync(Uri authorizationUri, CancellationToken ct)
    {
        var options = new WebAuthenticatorOptions
        {
            Url = authorizationUri,
            CallbackUrl = RedirectUri,
            PrefersEphemeralWebBrowserSession = false,
        };

        WebAuthenticatorResult result;
        try
        {
            result = await WebAuthenticator.Default.AuthenticateAsync(options).WaitAsync(ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // The user closed the browser tab without completing sign-in.
            return new Dictionary<string, string> { ["error"] = "access_denied", ["error_description"] = "Sign-in was cancelled." };
        }

        // Properties is everything WebAuthenticator parsed from the callback URI's query
        // string – for this authorization-code flow that's "code" and "state" on success,
        // or "error"/"error_description" if Keycloak rejected the request.
        return new Dictionary<string, string>(result.Properties);
    }
}
#endif
