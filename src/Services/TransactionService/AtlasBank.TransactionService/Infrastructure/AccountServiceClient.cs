namespace AtlasBank.TransactionService.Infrastructure;

public interface IAccountServiceClient
{
    Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default);
    Task<bool> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default);
}

public record AccountDto(Guid Id, Guid CustomerId, string AccountNumber, int Status, decimal Balance, string Currency);

public class AccountServiceClient(HttpClient http) : IAccountServiceClient
{
    public async Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"internal/accounts/{accountId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountDto>(ct);
    }

    public async Task<bool> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"internal/accounts/{accountId}/credit", new { Amount = amount }, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"internal/accounts/{accountId}/debit", new { Amount = amount }, ct);
        return response.IsSuccessStatusCode;
    }
}
