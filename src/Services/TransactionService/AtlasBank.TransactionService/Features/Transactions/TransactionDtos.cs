using AtlasBank.TransactionService.Domain.Enums;

namespace AtlasBank.TransactionService.Features.Transactions;

public record DepositRequest(Guid AccountId, decimal Amount, string? Description = null);

public record WithdrawRequest(Guid AccountId, decimal Amount, string? Description = null);

public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Description = null);

public record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? ToAccountId,
    TransactionType Type,
    TransactionStatus Status,
    decimal Amount,
    string Currency,
    string Reference,
    string? Description,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
