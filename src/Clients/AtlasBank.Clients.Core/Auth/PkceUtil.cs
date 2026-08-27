using System.Security.Cryptography;
using System.Text;

namespace AtlasBank.Clients.Core.Auth;

/// <summary>
/// RFC 7636 Proof Key for Code Exchange. Used instead of Keycloak's Resource Owner Password
/// grant (which the web client's directAccessGrantsEnabled flag still allows) so the user's
/// password never passes through this app's code — sign-in happens in the real browser, and
/// PKCE is what proves the token exchange afterward came from the same client that started it.
/// </summary>
public static class PkceUtil
{
    /// <summary>Generates a cryptographically random code verifier (43–128 chars, per RFC 7636 §4.1).</summary>
    public static string CreateCodeVerifier()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>Derives the S256 code challenge for a verifier (RFC 7636 §4.2).</summary>
    public static string CreateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    /// <summary>Generates an opaque value for the OAuth <c>state</c> parameter (CSRF protection).</summary>
    public static string CreateState()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
