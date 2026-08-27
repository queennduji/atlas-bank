using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Clients.Core.Api;

public sealed partial class AtlasApiClient
{
    public Task<IReadOnlyList<Account>> GetMyAccountsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Account>>("/api/accounts/me", ct);

    public Task<Account> GetAccountAsync(Guid accountId, CancellationToken ct = default) =>
        GetAsync<Account>($"/api/accounts/{accountId}", ct);

    public Task<Account> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default) =>
        PostAsync<Account>("/api/accounts", request, ct);
}
