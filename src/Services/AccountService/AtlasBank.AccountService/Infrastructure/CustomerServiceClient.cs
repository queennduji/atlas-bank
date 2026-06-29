namespace AtlasBank.AccountService.Infrastructure;

public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default);
}

public record CustomerDto(Guid Id, string FirstName, string LastName, string Email);

public class CustomerServiceClient(HttpClient http) : ICustomerServiceClient
{
    public async Task<CustomerDto?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/internal/customers/by-keycloak-id/{keycloakUserId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDto>(ct);
    }
}
