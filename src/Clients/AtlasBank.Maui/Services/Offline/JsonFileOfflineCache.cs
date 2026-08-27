using System.Text.Json;
using System.Text.Json.Serialization;
using AtlasBank.Clients.Core.Json;

namespace AtlasBank.Maui.Services.Offline;

public sealed class JsonFileOfflineCache : IOfflineCache
{
    private sealed record Envelope<T>(T Value, DateTimeOffset SavedAtUtc);

    public async Task SaveAsync<T>(string key, T value)
    {
        var envelope = new Envelope<T>(value, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(envelope, AtlasJsonOptions.Default);
        await File.WriteAllTextAsync(PathFor(key), json);
    }

    public async Task<(T Value, DateTimeOffset SavedAtUtc)?> LoadAsync<T>(string key)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var envelope = JsonSerializer.Deserialize<Envelope<T>>(json, AtlasJsonOptions.Default);
            return envelope is null ? null : (envelope.Value, envelope.SavedAtUtc);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // A corrupt or half-written cache file is worth ignoring, not crashing the dashboard over.
            return null;
        }
    }

    private static string PathFor(string key) =>
        Path.Combine(FileSystem.AppDataDirectory, $"cache-{key}.json");
}
