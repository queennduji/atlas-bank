namespace AtlasBank.Clients.Core.Models;

public sealed record DepositRequest
{
    public required Guid AccountId { get; init; }
    public required decimal Amount { get; init; }
    public string? Description { get; init; }
}

public sealed record WithdrawRequest
{
    public required Guid AccountId { get; init; }
    public required decimal Amount { get; init; }
    public string? Description { get; init; }
}

public sealed record TransferRequest
{
    public required Guid FromAccountId { get; init; }
    public required Guid ToAccountId { get; init; }
    public required decimal Amount { get; init; }
    public string? Description { get; init; }
}

public sealed record Transaction
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public Guid? ToAccountId { get; init; }
    public required TransactionType Type { get; init; }
    public required TransactionStatus Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Reference { get; init; }
    public string? Description { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
