using AtlasBank.LedgerService.Domain.Enums;

namespace AtlasBank.LedgerService.Features.Ledger;

public record LedgerEntryResponse(
    Guid Id,
    Guid TransactionId,
    Guid AccountId,
    LedgerEntryType Type,
    decimal Amount,
    string Currency,
    string Reference,
    string TransactionType,
    DateTimeOffset PostedAt,
    DateTimeOffset CreatedAt);

public record LedgerBalanceResponse(Guid AccountId, decimal Balance);
