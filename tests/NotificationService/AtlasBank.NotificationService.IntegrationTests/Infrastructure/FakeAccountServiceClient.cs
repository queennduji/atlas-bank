using AtlasBank.NotificationService.Infrastructure;

namespace AtlasBank.NotificationService.IntegrationTests.Infrastructure;

public class FakeAccountServiceClient : IAccountServiceClient
{
    public static readonly Guid AccountId = Guid.NewGuid();
    public static readonly Guid CustomerId = Guid.NewGuid();

    public Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        if (accountId == AccountId)
            return Task.FromResult<AccountDto?>(new AccountDto(AccountId, CustomerId, "ATL0000001", 0, 1000m, "USD"));

        return Task.FromResult<AccountDto?>(null);
    }
}
