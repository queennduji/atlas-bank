namespace AtlasBank.Clients.Core.Auth;

/// <summary>Config <see cref="OidcAuthenticator"/> needs for one Keycloak realm. One instance
/// per app – see AtlasBank.Maui's AppConfig for where these values actually come from.</summary>
public sealed record AtlasAuthOptions
{
    /// <summary>No trailing slash, e.g. "http://localhost:8080/realms/atlas-bank".</summary>
    public required string Authority { get; init; }

    public required string ClientId { get; init; }

    // No RedirectUri here – that's owned by IOAuthBrowserLauncher instead, since it's
    // platform-dependent (fixed scheme on mobile, a bound port on desktop).

    public string Scope { get; init; } = "openid profile email";
}
