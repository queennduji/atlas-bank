namespace AtlasBank.StatementService.Domain.Entities;

public class StatementLine
{
    public Guid Id { get; private set; }
    public Guid StatementId { get; private set; }
    public Guid TransactionId { get; private set; }
    public DateTimeOffset Date { get; private set; }
    public string Reference { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public decimal RunningBalance { get; private set; }

    private StatementLine() { }

    public static StatementLine Create(
        Guid statementId, Guid transactionId, DateTimeOffset date,
        string reference, string description, string type,
        decimal amount, decimal runningBalance) => new()
    {
        Id = Guid.NewGuid(),
        StatementId = statementId,
        TransactionId = transactionId,
        Date = date,
        Reference = reference,
        Description = description,
        Type = type,
        Amount = amount,
        RunningBalance = runningBalance
    };
}
