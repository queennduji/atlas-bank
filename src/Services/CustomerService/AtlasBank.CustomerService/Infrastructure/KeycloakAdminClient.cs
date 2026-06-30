using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtlasBank.CustomerService.Infrastructure;

public interface IKeycloakAdminClient
{
    Task<string> CreateUserAsync(string firstName, string lastName, string email, string password, CancellationToken ct = default);
}

public class KeycloakAdminClient(HttpClient http, IConfiguration config) : IKeycloakAdminClient
{
    public async Task<string> CreateUserAsync(string firstName, string lastName, string email, string password, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var payload = new
        {
            firstName,
            lastName,
            email,
            username = email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            },
            realmRoles = new[] { "user" }
        };

        var realm = config["Keycloak:Realm"];
        var response = await http.PostAsJsonAsync($"/admin/realms/{realm}/users", payload, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException("A user with this email already exists in Keycloak.");

        response.EnsureSuccessStatusCode();

        // Keycloak returns the new user ID in the Location header
        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak did not return a user location.");

        return location.Split('/').Last();
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        var adminUser = config["Keycloak:AdminUsername"];
        var adminPass = config["Keycloak:AdminPassword"];

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = adminUser!,
            ["password"] = adminPass!
        };

        var tokenResponse = await http.PostAsync(
            "/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(form), ct);

        tokenResponse.EnsureSuccessStatusCode();

        var json = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("access_token").GetString()!;
    }
}
