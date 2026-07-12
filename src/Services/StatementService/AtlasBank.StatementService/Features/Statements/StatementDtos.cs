namespace AtlasBank.StatementService.Features.Statements;

public record GenerateStatementRequest(Guid AccountId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd);

public record StatementLineResponse(
    Guid TransactionId, DateTimeOffset Date, string Reference,
    string Description, string Type, decimal Amount, decimal RunningBalance);

public record StatementResponse(
    Guid Id, Guid AccountId, Guid CustomerId,
    string AccountNumber, string CustomerName, string Currency,
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd,
    decimal OpeningBalance, decimal ClosingBalance,
    decimal TotalCredits, decimal TotalDebits,
    DateTimeOffset GeneratedAt, IReadOnlyList<StatementLineResponse> Lines);

public record StatementSummaryResponse(
    Guid Id, Guid AccountId, string AccountNumber,
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd,
    decimal ClosingBalance, DateTimeOffset GeneratedAt);
