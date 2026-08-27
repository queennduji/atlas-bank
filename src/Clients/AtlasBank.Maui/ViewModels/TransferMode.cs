namespace AtlasBank.Maui.ViewModels;

/// <summary>Which of the three money-movement endpoints TransferPage submits to. A UI-only
/// concept — there's no wire DTO for it, unlike AtlasBank.Clients.Core.Models.TransactionType.</summary>
public enum TransferMode
{
    Deposit,
    Withdraw,
    Transfer,
}
