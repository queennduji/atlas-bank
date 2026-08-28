using System.Net;
using System.Web;
using AtlasBank.Clients.Core.Auth;
using AtlasBank.Clients.Core.Tests.TestSupport;
using FluentAssertions;

namespace AtlasBank.Clients.Core.Tests.Auth;

public class OidcAuthenticatorTests
{
    private const string Authority = "http://keycloak.test/realms/atlas-bank";
    private const string AuthorizationEndpoint = "http://keycloak.test/realms/atlas-bank/protocol/openid-connect/auth";
    private const string TokenEndpoint = "http://keycloak.test/realms/atlas-bank/protocol/openid-connect/token";

    private static readonly string DiscoveryJson = $$"""
        { "authorization_endpoint": "{{AuthorizationEndpoint}}", "token_endpoint": "{{TokenEndpoint}}",
          "end_session_endpoint": "http://keycloak.test/realms/atlas-bank/protocol/openid-connect/logout" }
        """;

    private static (OidcAuthenticator Authenticator, FakeHttpMessageHandler Handler, FakeOAuthBrowserLauncher Launcher, InMemoryTokenStore Store)
        CreateAuthenticator(TokenSet? initialSession = null)
    {
        var handler = new FakeHttpMessageHandler();
        var launcher = new FakeOAuthBrowserLauncher();
        var store = new InMemoryTokenStore(initialSession);
        var authenticator = new OidcAuthenticator(
            new HttpClient(handler),
            new AtlasAuthOptions { Authority = Authority, ClientId = "atlas-bank-maui" },
            launcher,
            store);

        return (authenticator, handler, launcher, store);
    }

    private static HttpResponseMessage TokenResponse(string accessToken, string? refreshToken = "refresh-token", int expiresIn = 300)
    {
        var refreshTokenJson = refreshToken is null ? "null" : $"\"{refreshToken}\"";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""
                { "access_token": "{{accessToken}}", "refresh_token": {{refreshTokenJson}},
                  "id_token": "id-token", "expires_in": {{expiresIn}}, "token_type": "Bearer" }
                """, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private static void RouteDiscoveryAndToken(FakeHttpMessageHandler handler, Func<HttpRequestMessage, HttpResponseMessage> onToken)
    {
        handler.Handler = req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("openid-configuration"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(DiscoveryJson, System.Text.Encoding.UTF8, "application/json"),
                };
            }

            if (req.RequestUri!.AbsolutePath.EndsWith("/token"))
            {
                return onToken(req);
            }

            throw new InvalidOperationException($"Unexpected request to {req.RequestUri}");
        };
    }

    [Fact]
    public async Task SignInAsync_BuildsAnAuthorizationUrlWithPkceAndExchangesTheCodeForTokens()
    {
        var (authenticator, handler, launcher, store) = CreateAuthenticator();
        RouteDiscoveryAndToken(handler, _ => TokenResponse("new-access-token"));

        var tokens = await authenticator.SignInAsync();

        tokens.AccessToken.Should().Be("new-access-token");
        tokens.RefreshToken.Should().Be("refresh-token");
        authenticator.IsAuthenticated.Should().BeTrue();
        store.SaveCount.Should().Be(1);
        store.Saved!.AccessToken.Should().Be("new-access-token");

        var query = HttpUtility.ParseQueryString(launcher.LastAuthorizationUri!.Query);
        query["response_type"].Should().Be("code");
        query["client_id"].Should().Be("atlas-bank-maui");
        query["code_challenge_method"].Should().Be("S256");
        query["code_challenge"].Should().NotBeNullOrEmpty();
        query["state"].Should().NotBeNullOrEmpty();
        query["redirect_uri"].Should().Be(launcher.RedirectUri.ToString());
    }

    [Fact]
    public async Task SignInAsync_TalksToTheConfiguredAuthorityHost_EvenWhenDiscoveryClaimsADifferentOne()
    {
        // the actual bug that got reported: Keycloak always claims "localhost:8080"
        // (KC_HOSTNAME), but "localhost" inside the emulator isn't the host machine — the
        // authenticator has to ignore that and stick to whatever host it was configured
        // with (10.0.2.2 here)
        const string emulatorAuthority = "http://10.0.2.2:8080/realms/atlas-bank";
        var handler = new FakeHttpMessageHandler();
        var launcher = new FakeOAuthBrowserLauncher();
        var authenticator = new OidcAuthenticator(
            new HttpClient(handler),
            new AtlasAuthOptions { Authority = emulatorAuthority, ClientId = "atlas-bank-maui" },
            launcher,
            new InMemoryTokenStore());

        handler.Handler = req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("openid-configuration"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        { "authorization_endpoint": "http://localhost:8080/realms/atlas-bank/protocol/openid-connect/auth",
                          "token_endpoint": "http://localhost:8080/realms/atlas-bank/protocol/openid-connect/token" }
                        """, System.Text.Encoding.UTF8, "application/json"),
                };
            }

            return TokenResponse("access-token");
        };

        await authenticator.SignInAsync();

        launcher.LastAuthorizationUri!.Host.Should().Be("10.0.2.2");
        handler.Requests.Should().Contain(r => r.RequestUri!.Host == "10.0.2.2" && r.RequestUri!.AbsolutePath.EndsWith("/token"));
    }

    [Fact]
    public async Task SignInAsync_ExchangesTheCodeVerifierMatchingTheChallengeItSent()
    {
        // grabs the challenge from the authorization URL and the verifier from the token
        // exchange body so we can check they're actually consistent with each other
        string? capturedVerifier = null;
        string? capturedChallenge = null;

        var (authenticator, handler, launcher, _) = CreateAuthenticator();
        launcher.RespondWith = uri =>
        {
            capturedChallenge = HttpUtility.ParseQueryString(uri.Query)["code_challenge"];
            var state = HttpUtility.ParseQueryString(uri.Query)["state"] ?? string.Empty;
            return new Dictionary<string, string> { ["code"] = "fake-code", ["state"] = state };
        };
        RouteDiscoveryAndToken(handler, req =>
        {
            var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            capturedVerifier = HttpUtility.ParseQueryString(body)["code_verifier"];
            return TokenResponse("access-token");
        });

        await authenticator.SignInAsync();

        capturedVerifier.Should().NotBeNullOrEmpty();
        capturedChallenge.Should().Be(PkceUtil.CreateCodeChallenge(capturedVerifier!));
    }

    [Fact]
    public async Task SignInAsync_Throws_WhenStateDoesNotMatch()
    {
        var (authenticator, handler, launcher, _) = CreateAuthenticator();
        RouteDiscoveryAndToken(handler, _ => TokenResponse("access-token"));
        launcher.RespondWith = _ => new Dictionary<string, string> { ["code"] = "fake-code", ["state"] = "not-the-real-state" };

        var act = () => authenticator.SignInAsync();

        await act.Should().ThrowAsync<AuthException>().WithMessage("*state mismatch*");
    }

    [Fact]
    public async Task SignInAsync_Throws_WhenTheProviderReturnsAnError()
    {
        var (authenticator, handler, launcher, _) = CreateAuthenticator();
        RouteDiscoveryAndToken(handler, _ => TokenResponse("access-token"));
        launcher.RespondWith = _ => new Dictionary<string, string>
        {
            ["error"] = "access_denied",
            ["error_description"] = "User cancelled the login.",
        };

        var act = () => authenticator.SignInAsync();

        await act.Should().ThrowAsync<AuthException>().WithMessage("*User cancelled the login.*");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsTheCachedToken_WhenItIsNotStale()
    {
        var fresh = new TokenSet
        {
            AccessToken = "still-good",
            RefreshToken = "refresh-token",
            IdToken = null,
            AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
        };
        var (authenticator, handler, _, _) = CreateAuthenticator(fresh);

        var token = await authenticator.GetAccessTokenAsync();

        token.Should().Be("still-good");
        handler.Requests.Should().BeEmpty("a non-stale token shouldn't trigger discovery or a refresh call");
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshesAndRotatesTheRefreshToken_WhenTheCachedTokenIsStale()
    {
        var stale = new TokenSet
        {
            AccessToken = "old-access-token",
            RefreshToken = "old-refresh-token",
            IdToken = "old-id-token",
            AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
        };
        var (authenticator, handler, _, store) = CreateAuthenticator(stale);
        RouteDiscoveryAndToken(handler, _ => TokenResponse("rotated-access-token", refreshToken: "rotated-refresh-token"));

        var token = await authenticator.GetAccessTokenAsync();

        token.Should().Be("rotated-access-token");
        store.Saved!.RefreshToken.Should().Be("rotated-refresh-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_KeepsThePreviousRefreshToken_WhenTheResponseDoesNotIncludeANewOne()
    {
        var stale = new TokenSet
        {
            AccessToken = "old-access-token",
            RefreshToken = "old-refresh-token",
            IdToken = null,
            AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
        };
        var (authenticator, handler, _, store) = CreateAuthenticator(stale);
        RouteDiscoveryAndToken(handler, _ => TokenResponse("new-access-token", refreshToken: null));

        await authenticator.GetAccessTokenAsync();

        store.Saved!.RefreshToken.Should().Be("old-refresh-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ClearsTheSession_WhenTheRefreshTokenIsRejected()
    {
        var stale = new TokenSet
        {
            AccessToken = "old-access-token",
            RefreshToken = "revoked-refresh-token",
            IdToken = null,
            AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
        };
        var (authenticator, handler, _, store) = CreateAuthenticator(stale);
        RouteDiscoveryAndToken(handler, _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{ "error": "invalid_grant", "error_description": "Refresh token expired." }""",
                System.Text.Encoding.UTF8, "application/json"),
        });

        var token = await authenticator.GetAccessTokenAsync();

        token.Should().BeNull();
        store.ClearCount.Should().Be(1);
        authenticator.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task TryRestoreSessionAsync_ReturnsNull_WhenNothingWasSaved()
    {
        var (authenticator, _, _, _) = CreateAuthenticator(initialSession: null);

        (await authenticator.TryRestoreSessionAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SignOutAsync_ClearsBothTheCacheAndTheStore()
    {
        var (authenticator, handler, launcher, store) = CreateAuthenticator();
        RouteDiscoveryAndToken(handler, _ => TokenResponse("access-token"));
        await authenticator.SignInAsync();

        await authenticator.SignOutAsync();

        authenticator.IsAuthenticated.Should().BeFalse();
        store.Saved.Should().BeNull();
        (await authenticator.GetAccessTokenAsync()).Should().BeNull();
    }
}
