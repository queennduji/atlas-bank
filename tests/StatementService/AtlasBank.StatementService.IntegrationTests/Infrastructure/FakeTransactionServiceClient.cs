using AtlasBank.StatementService.Infrastructure;

namespace AtlasBank.StatementService.IntegrationTests.Infrastructure;

public class FakeTransactionServiceClient : ITransactionServiceClient
{
    private readonly List<TransactionDto> _transactions = [];

    public void Seed(IEnumerable<TransactionDto> transactions) => _transactions.AddRange(transactions);

    public void Clear() => _transactions.Clear();

    public Task<IReadOnlyList<TransactionDto>> GetByAccountAsync(
        Guid accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var result = _transactions
            .Where(t => (t.AccountId == accountId || t.ToAccountId == accountId)
                     && t.CreatedAt >= from && t.CreatedAt <= to)
            .ToList();

        return Task.FromResult<IReadOnlyList<TransactionDto>>(result);
    }

    public static TransactionDto MakeDeposit(Guid accountId, decimal amount, string description = "Deposit",
        DateTimeOffset? at = null)
        => new(Guid.NewGuid(), accountId, null, "Deposit", "Completed",
               amount, "USD", $"REF-{Guid.NewGuid():N}"[..12], description,
               at ?? DateTimeOffset.UtcNow, at ?? DateTimeOffset.UtcNow);

    public static TransactionDto MakeWithdrawal(Guid accountId, decimal amount, string description = "Withdrawal",
        DateTimeOffset? at = null)
        => new(Guid.NewGuid(), accountId, null, "Withdrawal", "Completed",
               amount, "USD", $"REF-{Guid.NewGuid():N}"[..12], description,
               at ?? DateTimeOffset.UtcNow, at ?? DateTimeOffset.UtcNow);

    public static TransactionDto MakeTransferOut(Guid fromAccountId, Guid toAccountId, decimal amount,
        DateTimeOffset? at = null)
        => new(Guid.NewGuid(), fromAccountId, toAccountId, "Transfer", "Completed",
               amount, "USD", $"REF-{Guid.NewGuid():N}"[..12], "Transfer",
               at ?? DateTimeOffset.UtcNow, at ?? DateTimeOffset.UtcNow);

    public static TransactionDto MakeTransferIn(Guid fromAccountId, Guid toAccountId, decimal amount,
        DateTimeOffset? at = null)
        => new(Guid.NewGuid(), fromAccountId, toAccountId, "Transfer", "Completed",
               amount, "USD", $"REF-{Guid.NewGuid():N}"[..12], "Transfer",
               at ?? DateTimeOffset.UtcNow, at ?? DateTimeOffset.UtcNow);
}
