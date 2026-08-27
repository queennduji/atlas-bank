using System.Globalization;
using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Maui.ViewModels;

/// <summary>
/// A <see cref="Transaction"/> plus the one thing it doesn't tell you on its own: whether,
/// from the account this row is shown on, money moved in or out. Deposit is always a
/// credit, Withdrawal always a debit — Transfer is only a credit if the viewed account is
/// the *receiving* side (<see cref="Transaction.ToAccountId"/>).
/// </summary>
public sealed record TransactionRow(Transaction Transaction, bool IsCredit)
{
    public string SignedAmountText =>
        $"{(IsCredit ? "+" : "-")}{Transaction.Amount.ToString("N2", CultureInfo.InvariantCulture)} {Transaction.Currency}";

    public static TransactionRow For(Transaction transaction, Guid viewedAccountId)
    {
        var isCredit = transaction.Type == TransactionType.Deposit
            || (transaction.Type == TransactionType.Transfer && transaction.ToAccountId == viewedAccountId);
        return new TransactionRow(transaction, isCredit);
    }
}
