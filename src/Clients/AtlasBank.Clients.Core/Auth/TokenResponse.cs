using System.Text.Json.Serialization;

namespace AtlasBank.Clients.Core.Auth;

/// <summary>Raw shape of Keycloak's token endpoint response (RFC 6749 §5.1) — snake_case on the wire.</summary>
internal sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresInSeconds { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}

/// <summary>OAuth error response shape (RFC 6749 §5.2) — returned by Keycloak's token endpoint
/// with a 400 when a code or refresh token is rejected.</summary>
internal sealed record OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}

/// <summary>The subset of a Keycloak/OIDC discovery document (<c>/.well-known/openid-configuration</c>)
/// this library needs.</summary>
internal sealed record OidcDiscoveryDocument
{
    [JsonPropertyName("authorization_endpoint")]
    public required string AuthorizationEndpoint { get; init; }

    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    [JsonPropertyName("end_session_endpoint")]
    public string? EndSessionEndpoint { get; init; }
}
