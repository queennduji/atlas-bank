using AtlasBank.TransactionService.Infrastructure;

namespace AtlasBank.TransactionService.IntegrationTests.Infrastructure;

public class FakeAccountServiceClient : IAccountServiceClient
{
    public static readonly Guid ActiveAccountId = Guid.NewGuid();
    public static readonly Guid SecondAccountId = Guid.NewGuid();
    public static readonly Guid FrozenAccountId = Guid.NewGuid();

    private readonly Dictionary<Guid, decimal> _balances = new()
    {
        [ActiveAccountId] = 1000m,
        [SecondAccountId] = 500m,
        [FrozenAccountId] = 200m,
    };

    public Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        if (accountId == ActiveAccountId)
            return Task.FromResult<AccountDto?>(new AccountDto(ActiveAccountId, Guid.NewGuid(), "ATL0000001", 0, _balances[ActiveAccountId], "USD"));
        if (accountId == SecondAccountId)
            return Task.FromResult<AccountDto?>(new AccountDto(SecondAccountId, Guid.NewGuid(), "ATL0000002", 0, _balances[SecondAccountId], "USD"));
        if (accountId == FrozenAccountId)
            return Task.FromResult<AccountDto?>(new AccountDto(FrozenAccountId, Guid.NewGuid(), "ATL0000003", 1, _balances[FrozenAccountId], "USD")); // status=1 = Frozen
        return Task.FromResult<AccountDto?>(null);
    }

    public Task<bool> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default)
    {
        if (!_balances.ContainsKey(accountId)) return Task.FromResult(false);
        _balances[accountId] += amount;
        return Task.FromResult(true);
    }

    public Task<bool> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default)
    {
        if (!_balances.ContainsKey(accountId)) return Task.FromResult(false);
        if (_balances[accountId] < amount) return Task.FromResult(false);
        _balances[accountId] -= amount;
        return Task.FromResult(true);
    }

    public decimal GetBalance(Guid accountId) => _balances.GetValueOrDefault(accountId);
}
