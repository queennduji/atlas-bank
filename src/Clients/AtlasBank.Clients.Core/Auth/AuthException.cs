namespace AtlasBank.Clients.Core.Auth;

/// <summary>Something went wrong signing in: the user denied consent, the redirect's
/// <c>state</c> didn't match, or Keycloak rejected the code/refresh token exchange.</summary>
public sealed class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
    public AuthException(string message, Exception innerException) : base(message, innerException) { }
}
