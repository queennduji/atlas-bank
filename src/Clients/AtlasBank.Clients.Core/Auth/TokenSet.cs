namespace AtlasBank.Clients.Core.Auth;

/// <summary>A Keycloak token response, plus the wall-clock expiry it implies.</summary>
public sealed record TokenSet
{
    public required string AccessToken { get; init; }
    public required string? RefreshToken { get; init; }
    public required string? IdToken { get; init; }
    public required DateTimeOffset AccessTokenExpiresAtUtc { get; init; }

    /// <summary>True once the token is within its refresh window (or already expired) – refreshed
    /// proactively rather than waiting for the gateway to return a 401 first.</summary>
    public bool NeedsRefresh(TimeProvider clock) =>
        clock.GetUtcNow() >= AccessTokenExpiresAtUtc - RefreshSkew;

    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);
}
