namespace AtlasBank.Maui.Config;

/// <summary>
/// Where this app finds the API Gateway and Keycloak. MAUI doesn't have anything like the
/// frontend's Vite env vars, and there's an extra wrinkle here that Docker doesn't have: the
/// Android emulator's virtual NIC maps the host machine's localhost to 10.0.2.2, not 127.0.0.1.
/// </summary>
public static class AppConfig
{
#if ANDROID
    public const string GatewayBaseUrl = "http://10.0.2.2:5000";
    public const string KeycloakAuthority = "http://10.0.2.2:8080/realms/atlas-bank";
#else
    // iOS/MacCatalyst simulators and Windows can all reach the host's loopback directly.
    public const string GatewayBaseUrl = "http://localhost:5000";
    public const string KeycloakAuthority = "http://localhost:8080/realms/atlas-bank";
#endif

    /// <summary>Matches the "atlas-bank-maui" client in keycloak/realm-export.json – kept
    /// separate from the web's "atlas-bank-app" so a stolen mobile refresh token can be
    /// revoked without logging every browser session out too.</summary>
    public const string KeycloakClientId = "atlas-bank-maui";

    /// <summary>URL scheme mobile registers to catch Keycloak's redirect – see
    /// Platforms/Android/OAuthCallbackActivity.cs and the iOS/MacCatalyst Info.plist entries.</summary>
    public const string MobileRedirectScheme = "atlasbank";
}
