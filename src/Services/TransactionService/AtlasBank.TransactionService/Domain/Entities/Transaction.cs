using AtlasBank.TransactionService.Domain.Enums;

namespace AtlasBank.TransactionService.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? ToAccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string Reference { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private Transaction() { }

    public static Transaction CreateDeposit(Guid accountId, decimal amount, string currency, string? description = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Pending,
            Amount = amount,
            Currency = currency,
            Reference = GenerateReference(),
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Transaction CreateWithdrawal(Guid accountId, decimal amount, string currency, string? description = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Pending,
            Amount = amount,
            Currency = currency,
            Reference = GenerateReference(),
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Transaction CreateTransfer(Guid fromAccountId, Guid toAccountId, decimal amount, string currency, string? description = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = TransactionType.Transfer,
            Status = TransactionStatus.Pending,
            Amount = amount,
            Currency = currency,
            Reference = GenerateReference(),
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Complete()
    {
        Status = TransactionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = TransactionStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private static string GenerateReference()
        => string.Concat("TXN", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()[^8..],
            Random.Shared.Next(100, 999).ToString());
}
