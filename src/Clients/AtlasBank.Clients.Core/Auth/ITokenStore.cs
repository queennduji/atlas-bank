namespace AtlasBank.Clients.Core.Auth;

/// <summary>
/// Persists tokens between app launches. Each UI project provides its own implementation on
/// top of whatever secure storage that platform has – MAUI's <c>SecureStorage</c>, DPAPI for
/// WPF – so tokens are never written to disk in plain text.
/// </summary>
public interface ITokenStore
{
    Task SaveAsync(TokenSet tokens);
    Task<TokenSet?> LoadAsync();
    Task ClearAsync();
}
