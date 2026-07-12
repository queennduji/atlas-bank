namespace AtlasBank.StatementService.Infrastructure;

public record TransactionDto(
    Guid Id, Guid AccountId, Guid? ToAccountId,
    string Type, string Status, decimal Amount, string Currency,
    string Reference, string? Description,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public interface ITransactionServiceClient
{
    Task<IReadOnlyList<TransactionDto>> GetByAccountAsync(
        Guid accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
