namespace AtlasBank.Clients.Core.Models;

public sealed record GenerateStatementRequest
{
    public required Guid AccountId { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
}

public sealed record StatementLine
{
    public required Guid TransactionId { get; init; }
    public required DateTimeOffset Date { get; init; }
    public required string Reference { get; init; }
    public required string Description { get; init; }

    /// <summary>Transaction type as a display string (server-rendered, not the numeric enum).</summary>
    public required string Type { get; init; }

    public required decimal Amount { get; init; }
    public required decimal RunningBalance { get; init; }
}

public sealed record Statement
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string AccountNumber { get; init; }
    public required string CustomerName { get; init; }
    public required string Currency { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required decimal OpeningBalance { get; init; }
    public required decimal ClosingBalance { get; init; }
    public required decimal TotalCredits { get; init; }
    public required decimal TotalDebits { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public required IReadOnlyList<StatementLine> Lines { get; init; }
}

public sealed record StatementSummary
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountNumber { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required decimal ClosingBalance { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}
