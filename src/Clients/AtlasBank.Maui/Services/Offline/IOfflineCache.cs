namespace AtlasBank.Maui.Services.Offline;

/// <summary>
/// Keeps the last successful response for a couple of read endpoints so the dashboard shows
/// something (clearly marked as stale) when a phone loses signal, instead of an empty
/// screen. Not a general offline-write layer – deposits, transfers, and card actions still
/// need connectivity, same as the web app.
/// </summary>
public interface IOfflineCache
{
    Task SaveAsync<T>(string key, T value);
    Task<(T Value, DateTimeOffset SavedAtUtc)?> LoadAsync<T>(string key);
}
