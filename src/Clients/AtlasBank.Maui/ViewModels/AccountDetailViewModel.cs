using System.Collections.ObjectModel;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Maui.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

[QueryProperty(nameof(AccountId), "accountId")]
public partial class AccountDetailViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChecking))]
    private Guid accountId;

    [ObservableProperty]
    private Account? account;

    public ObservableCollection<TransactionRow> Transactions { get; } = [];

    public bool HasTransactions => Transactions.Count > 0;
    public bool IsChecking => Account?.Type == AccountType.Checking;

    public AccountDetailViewModel(AtlasApiClient api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
        Transactions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTransactions));
    }

    async partial void OnAccountIdChanged(Guid value)
    {
        if (value != Guid.Empty)
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var accountTask = _api.GetAccountAsync(AccountId);
        var transactionsTask = _api.GetAccountTransactionsAsync(AccountId);
        await Task.WhenAll(accountTask, transactionsTask);

        Account = accountTask.Result;
        OnPropertyChanged(nameof(IsChecking));

        Transactions.Clear();
        foreach (var transaction in transactionsTask.Result.OrderByDescending(t => t.CreatedAt))
        {
            Transactions.Add(TransactionRow.For(transaction, AccountId));
        }
    });

    [RelayCommand]
    private Task DepositAsync() => GoToTransferAsync("deposit");

    [RelayCommand]
    private Task WithdrawAsync() => GoToTransferAsync("withdraw");

    [RelayCommand]
    private Task TransferAsync() => GoToTransferAsync("transfer");

    private Task GoToTransferAsync(string mode) => _navigation.GoToAsync(Routes.Transfer, new Dictionary<string, object>
    {
        ["fromAccountId"] = AccountId,
        ["mode"] = mode,
    });

    [RelayCommand]
    private Task ViewStatementsAsync() =>
        _navigation.GoToRootAsync(Routes.TabRoute(Routes.Statements), new Dictionary<string, object> { ["accountId"] = AccountId });
}
