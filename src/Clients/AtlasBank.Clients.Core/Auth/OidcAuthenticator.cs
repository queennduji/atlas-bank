using System.Net.Http.Json;
using System.Web;
using AtlasBank.Clients.Core.Json;

namespace AtlasBank.Clients.Core.Auth;

/// <summary>
/// Runs the Authorization Code + PKCE flow against Keycloak and keeps the tokens fresh
/// afterward. Every AtlasBank client shares this class as-is – only
/// <see cref="IOAuthBrowserLauncher"/> (how to pop the browser) and <see cref="ITokenStore"/>
/// (where tokens are kept) change per platform.
/// </summary>
public sealed class OidcAuthenticator
{
    private readonly HttpClient _http;
    private readonly AtlasAuthOptions _options;
    private readonly IOAuthBrowserLauncher _browserLauncher;
    private readonly ITokenStore _tokenStore;
    private readonly TimeProvider _clock;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private OidcDiscoveryDocument? _discovery;
    private TokenSet? _current;

    public OidcAuthenticator(
        HttpClient http,
        AtlasAuthOptions options,
        IOAuthBrowserLauncher browserLauncher,
        ITokenStore tokenStore,
        TimeProvider? clock = null)
    {
        _http = http;
        _options = options;
        _browserLauncher = browserLauncher;
        _tokenStore = tokenStore;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>True once a token has been loaded or acquired this session.</summary>
    public bool IsAuthenticated => _current is not null;

    /// <summary>Loads a previously saved session, refreshing it if its access token is stale.
    /// Returns null (rather than throwing) if there's nothing to restore or the refresh token
    /// itself has expired – callers treat that the same as "never signed in".</summary>
    public async Task<TokenSet?> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        var saved = await _tokenStore.LoadAsync().ConfigureAwait(false);
        if (saved is null)
        {
            return null;
        }

        _current = saved;
        if (!saved.NeedsRefresh(_clock))
        {
            return saved;
        }

        try
        {
            return await RefreshAsync(ct).ConfigureAwait(false);
        }
        catch (AuthException)
        {
            // Refresh token expired or was revoked server-side – the saved session is dead.
            await SignOutAsync().ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>Runs the full interactive sign-in: opens the browser, exchanges the returned
    /// code for tokens, and persists the result.</summary>
    public async Task<TokenSet> SignInAsync(CancellationToken ct = default)
    {
        var discovery = await GetDiscoveryAsync(ct).ConfigureAwait(false);

        var codeVerifier = PkceUtil.CreateCodeVerifier();
        var codeChallenge = PkceUtil.CreateCodeChallenge(codeVerifier);
        var state = PkceUtil.CreateState();
        var redirectUri = _browserLauncher.RedirectUri;

        var authorizeQuery = HttpUtility.ParseQueryString(string.Empty);
        authorizeQuery["response_type"] = "code";
        authorizeQuery["client_id"] = _options.ClientId;
        authorizeQuery["redirect_uri"] = redirectUri.ToString();
        authorizeQuery["scope"] = _options.Scope;
        authorizeQuery["state"] = state;
        authorizeQuery["code_challenge"] = codeChallenge;
        authorizeQuery["code_challenge_method"] = "S256";

        var authorizationUri = new UriBuilder(discovery.AuthorizationEndpoint) { Query = authorizeQuery.ToString() }.Uri;

        var callback = await _browserLauncher.AuthenticateAsync(authorizationUri, ct).ConfigureAwait(false);

        if (callback.TryGetValue("error", out var error))
        {
            var description = callback.GetValueOrDefault("error_description", error);
            throw new AuthException($"Sign-in was not completed: {description}");
        }

        if (!callback.TryGetValue("state", out var returnedState) || returnedState != state)
        {
            throw new AuthException("Sign-in response failed CSRF validation (state mismatch).");
        }

        if (!callback.TryGetValue("code", out var code))
        {
            throw new AuthException("Sign-in response did not include an authorization code.");
        }

        var tokenResponse = await ExchangeAsync(discovery, new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri.ToString(),
            ["code_verifier"] = codeVerifier,
        }, ct).ConfigureAwait(false);

        var tokens = ToTokenSet(tokenResponse);
        _current = tokens;
        await _tokenStore.SaveAsync(tokens).ConfigureAwait(false);
        return tokens;
    }

    /// <summary>Returns a valid access token for an API call, refreshing first if the cached
    /// one is stale or <paramref name="forceRefresh"/> is set (e.g. after a 401). Returns null
    /// if there's no session at all.</summary>
    public async Task<string?> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (_current is null)
        {
            _current = await _tokenStore.LoadAsync().ConfigureAwait(false);
        }

        if (_current is null)
        {
            return null;
        }

        if (forceRefresh || _current.NeedsRefresh(_clock))
        {
            try
            {
                await RefreshAsync(ct).ConfigureAwait(false);
            }
            catch (AuthException)
            {
                await SignOutAsync().ConfigureAwait(false);
                return null;
            }
        }

        return _current?.AccessToken;
    }

    public async Task SignOutAsync()
    {
        _current = null;
        await _tokenStore.ClearAsync().ConfigureAwait(false);
    }

    /// <summary>Builds Keycloak's front-channel logout URL, for callers that want to also end
    /// the browser's SSO session (not just this app's local one) by opening it in the system browser.</summary>
    public async Task<Uri?> BuildEndSessionUriAsync(Uri postLogoutRedirectUri, CancellationToken ct = default)
    {
        var discovery = await GetDiscoveryAsync(ct).ConfigureAwait(false);
        if (discovery.EndSessionEndpoint is null)
        {
            return null;
        }

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = _options.ClientId;
        query["post_logout_redirect_uri"] = postLogoutRedirectUri.ToString();
        if (_current?.IdToken is { } idToken)
        {
            query["id_token_hint"] = idToken;
        }

        return new UriBuilder(discovery.EndSessionEndpoint) { Query = query.ToString() }.Uri;
    }

    private async Task<TokenSet> RefreshAsync(CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Another caller may have already refreshed while we waited for the lock.
            if (_current is { } current && !current.NeedsRefresh(_clock))
            {
                return current;
            }

            if (_current?.RefreshToken is not { } refreshToken)
            {
                throw new AuthException("No refresh token available.");
            }

            var discovery = await GetDiscoveryAsync(ct).ConfigureAwait(false);
            var tokenResponse = await ExchangeAsync(discovery, new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _options.ClientId,
                ["refresh_token"] = refreshToken,
            }, ct).ConfigureAwait(false);

            var tokens = ToTokenSet(tokenResponse);
            _current = tokens;
            await _tokenStore.SaveAsync(tokens).ConfigureAwait(false);
            return tokens;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<TokenResponse> ExchangeAsync(OidcDiscoveryDocument discovery, Dictionary<string, string> parameters, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, discovery.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(parameters),
        };

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponse>(AtlasJsonOptions.Default, ct).ConfigureAwait(false);
            throw new AuthException(error?.ErrorDescription ?? error?.Error ?? $"Token endpoint returned {(int)response.StatusCode}.");
        }

        return (await response.Content.ReadFromJsonAsync<TokenResponse>(AtlasJsonOptions.Default, ct).ConfigureAwait(false))!;
    }

    private async Task<OidcDiscoveryDocument> GetDiscoveryAsync(CancellationToken ct)
    {
        if (_discovery is not null)
        {
            return _discovery;
        }

        var discoveryUri = $"{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";
        var document = await _http.GetFromJsonAsync<OidcDiscoveryDocument>(discoveryUri, AtlasJsonOptions.Default, ct).ConfigureAwait(false)
            ?? throw new AuthException("Could not load Keycloak's discovery document.");

        // KC_HOSTNAME pins this realm to always claim "localhost:8080" in its endpoints, no
        // matter who asked – fine for the web app, broken for the Android emulator, where
        // "localhost" means the emulator itself, not the host machine. Trust the paths
        // Keycloak gave us, but always talk to the host we were actually configured with.
        _discovery = RewriteToConfiguredAuthority(document);
        return _discovery;
    }

    private OidcDiscoveryDocument RewriteToConfiguredAuthority(OidcDiscoveryDocument document)
    {
        var authority = new Uri(_options.Authority);
        return document with
        {
            AuthorizationEndpoint = RewriteHost(document.AuthorizationEndpoint, authority),
            TokenEndpoint = RewriteHost(document.TokenEndpoint, authority),
            EndSessionEndpoint = document.EndSessionEndpoint is null ? null : RewriteHost(document.EndSessionEndpoint, authority),
        };
    }

    private static string RewriteHost(string endpoint, Uri authority) =>
        new UriBuilder(endpoint) { Scheme = authority.Scheme, Host = authority.Host, Port = authority.Port }.Uri.ToString();

    private TokenSet ToTokenSet(TokenResponse response) => new()
    {
        AccessToken = response.AccessToken,
        // Keycloak rotates the refresh token on every refresh, but not every grant does –
        // keep the old one if this response didn't send a new one.
        RefreshToken = response.RefreshToken ?? _current?.RefreshToken,
        IdToken = response.IdToken ?? _current?.IdToken,
        AccessTokenExpiresAtUtc = _clock.GetUtcNow().AddSeconds(response.ExpiresInSeconds),
    };
}
