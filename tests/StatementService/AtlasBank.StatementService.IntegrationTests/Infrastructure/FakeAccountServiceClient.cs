using AtlasBank.StatementService.Infrastructure;

namespace AtlasBank.StatementService.IntegrationTests.Infrastructure;

public class FakeAccountServiceClient : IAccountServiceClient
{
    public static readonly Guid AccountId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid CustomerId = FakeCustomerServiceClient.CustomerId;

    public Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        if (accountId == AccountId)
            return Task.FromResult<AccountDto?>(
                new AccountDto(AccountId, CustomerId, "ATL0000001", "USD", 1500m));

        return Task.FromResult<AccountDto?>(null);
    }
}
