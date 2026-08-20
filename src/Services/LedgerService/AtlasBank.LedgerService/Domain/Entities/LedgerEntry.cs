using AtlasBank.LedgerService.Domain.Enums;

namespace AtlasBank.LedgerService.Domain.Entities;

/// <summary>
/// A single-sided, append-only ledger posting. A deposit or withdrawal produces one entry;
/// a transfer produces two (a debit on the source account, a credit on the destination
/// account) from the same <c>TransactionCompletedEvent</c> — standard double-entry
/// bookkeeping, where every movement of money is recorded from both sides.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public LedgerEntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string Reference { get; private set; } = default!;
    public string TransactionType { get; private set; } = default!;
    public DateTimeOffset PostedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private LedgerEntry() { }

    public static LedgerEntry Create(
        Guid transactionId,
        Guid accountId,
        LedgerEntryType type,
        decimal amount,
        string currency,
        string reference,
        string transactionType,
        DateTimeOffset postedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Currency = currency,
            Reference = reference,
            TransactionType = transactionType,
            PostedAt = postedAt,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
