using System.Collections.ObjectModel;
using System.Globalization;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Maui.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

[QueryProperty(nameof(FromAccountId), "fromAccountId")]
[QueryProperty(nameof(ModeText), "mode")]
public partial class TransferViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;
    private readonly INavigationService _navigation;

    // one key per screen visit – a retried SubmitAsync (network blip) reuses it so the
    // gateway treats it as the same request, but opening this page again for a new transfer
    // gets a fresh one
    private readonly string _idempotencyKey = Guid.NewGuid().ToString();

    [ObservableProperty]
    private Guid fromAccountId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private TransferMode mode;

    [ObservableProperty]
    private Account? fromAccount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private Account? toAccount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string amountText = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isComplete;

    public ObservableCollection<Account> OtherAccounts { get; } = [];

    public string ModeText
    {
        get => Mode.ToString().ToLowerInvariant();
        set
        {
            if (Enum.TryParse<TransferMode>(value, ignoreCase: true, out var parsed))
            {
                Mode = parsed;
            }
        }
    }

    public bool IsTransfer => Mode == TransferMode.Transfer;

    public string Title => Mode switch
    {
        TransferMode.Deposit => "Deposit",
        TransferMode.Withdraw => "Withdraw",
        TransferMode.Transfer => "Transfer",
        _ => "Move money",
    };

    public TransferViewModel(AtlasApiClient api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    async partial void OnFromAccountIdChanged(Guid value)
    {
        if (value == Guid.Empty)
        {
            return;
        }

        await RunAsync(async () =>
        {
            var accounts = await _api.GetMyAccountsAsync();
            FromAccount = accounts.FirstOrDefault(a => a.Id == value);

            OtherAccounts.Clear();
            foreach (var account in accounts.Where(a => a.Id != value))
            {
                OtherAccounts.Add(account);
            }
        });
    }

    partial void OnModeChanged(TransferMode value) => OnPropertyChanged(nameof(IsTransfer));

    private bool CanSubmit() =>
        !IsBusy
        && decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
        && amount > 0
        && (Mode != TransferMode.Transfer || ToAccount is not null);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private Task SubmitAsync() => RunAsync(async () =>
    {
        var amount = decimal.Parse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture);
        var description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

        _ = Mode switch
        {
            TransferMode.Deposit => await _api.DepositAsync(
                new DepositRequest { AccountId = FromAccountId, Amount = amount, Description = description },
                _idempotencyKey),
            TransferMode.Withdraw => await _api.WithdrawAsync(
                new WithdrawRequest { AccountId = FromAccountId, Amount = amount, Description = description },
                _idempotencyKey),
            TransferMode.Transfer => await _api.TransferAsync(
                new TransferRequest { FromAccountId = FromAccountId, ToAccountId = ToAccount!.Id, Amount = amount, Description = description },
                _idempotencyKey),
            _ => throw new InvalidOperationException($"Unknown transfer mode: {Mode}"),
        };

        IsComplete = true;
    });

    [RelayCommand]
    private Task CloseAsync() => _navigation.GoBackAsync();

    partial void OnAmountTextChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
}
