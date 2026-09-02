using System.Text.Json;
using AtlasBank.Clients.Core.Auth;
using AtlasBank.Clients.Core.Json;

namespace AtlasBank.Maui.Services.Auth;

/// <summary>
/// Persists the session via <see cref="SecureStorage"/> – Android Keystore, iOS/MacCatalyst
/// Keychain, or Windows Credential Locker, depending on the platform. The refresh token
/// never ends up sitting on disk in plain text the way a Preferences-based store would.
/// </summary>
public sealed class MauiSecureTokenStore : ITokenStore
{
    private const string StorageKey = "atlasbank.session";

    public async Task SaveAsync(TokenSet tokens)
    {
        var json = JsonSerializer.Serialize(tokens, AtlasJsonOptions.Default);
        await SecureStorage.Default.SetAsync(StorageKey, json).ConfigureAwait(false);
    }

    public async Task<TokenSet?> LoadAsync()
    {
        string? json;
        try
        {
            json = await SecureStorage.Default.GetAsync(StorageKey).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Platform keystores can throw if the app's signing key changed (e.g. a fresh
            // debug install) and the previously encrypted blob can no longer be opened –
            // treat that the same as "no saved session" rather than crashing at startup.
            return null;
        }

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TokenSet>(json, AtlasJsonOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(StorageKey);
        return Task.CompletedTask;
    }
}
