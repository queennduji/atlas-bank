namespace AtlasBank.StatementService.Domain.Entities;

public class Statement
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string AccountNumber { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public List<StatementLine> Lines { get; private set; } = [];

    private Statement() { }

    public static Statement Generate(
        Guid accountId, Guid customerId, string accountNumber,
        string customerName, string currency,
        DateTimeOffset periodStart, DateTimeOffset periodEnd,
        decimal openingBalance, IEnumerable<(Guid txId, DateTimeOffset date, string reference, string? description, string type, decimal amount)> transactions)
    {
        var statement = new Statement
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CustomerId = customerId,
            AccountNumber = accountNumber,
            CustomerName = customerName,
            Currency = currency,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            OpeningBalance = openingBalance,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        var runningBalance = openingBalance;
        decimal totalCredits = 0, totalDebits = 0;

        foreach (var tx in transactions)
        {
            // Transfer credited to this account when ToAccountId matches — callers pass unsigned amount
            var isCredit = tx.type is "Deposit" or "TransferIn";
            runningBalance += isCredit ? tx.amount : -tx.amount;
            if (isCredit) totalCredits += tx.amount;
            else totalDebits += tx.amount;

            statement.Lines.Add(StatementLine.Create(
                statement.Id, tx.txId, tx.date,
                tx.reference, tx.description ?? "-",
                tx.type, tx.amount, runningBalance));
        }

        statement.ClosingBalance = runningBalance;
        statement.TotalCredits = totalCredits;
        statement.TotalDebits = totalDebits;

        return statement;
    }
}
