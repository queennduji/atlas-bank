using System.Text.Json;

namespace AtlasBank.Clients.Core.Json;

/// <summary>
/// The one <see cref="JsonSerializerOptions"/> every request/response in this library uses –
/// camelCase to match ASP.NET Core's default, case-insensitive on the way in so a stray
/// casing mismatch doesn't just silently deserialize to null.
/// </summary>
public static class AtlasJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
}
