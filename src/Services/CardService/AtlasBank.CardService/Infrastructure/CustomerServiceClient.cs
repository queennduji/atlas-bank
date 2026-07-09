using System.Net.Http.Json;

namespace AtlasBank.CardService.Infrastructure;

public class CustomerServiceClient(HttpClient httpClient) : ICustomerServiceClient
{
    public async Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/internal/customers/{customerId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CustomerDto>(ct);
    }
}
