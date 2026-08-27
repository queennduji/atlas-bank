using System.Collections.ObjectModel;
using System.Globalization;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

public partial class CardsViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;

    public ObservableCollection<Account> Accounts { get; } = [];
    public ObservableCollection<Card> Cards { get; } = [];
    public IReadOnlyList<CardType> CardTypes { get; } = Enum.GetValues<CardType>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAccount))]
    [NotifyCanExecuteChangedFor(nameof(IssueCardCommand))]
    private Account? selectedAccount;

    [ObservableProperty]
    private CardType issueCardType = CardType.Debit;

    [ObservableProperty]
    private string issueSpendingLimitText = "1000";

    [ObservableProperty]
    private Card? selectedCard;

    [ObservableProperty]
    private string editSpendingLimitText = string.Empty;

    public bool HasSelectedAccount => SelectedAccount is not null;
    public bool HasCards => Cards.Count > 0;

    public CardsViewModel(AtlasApiClient api)
    {
        _api = api;
        Cards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCards));
    }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var accounts = await _api.GetMyAccountsAsync();
        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(account);
        }

        SelectedAccount ??= Accounts.FirstOrDefault();
        if (SelectedAccount is not null)
        {
            await LoadCardsAsync(SelectedAccount.Id);
        }
    });

    async partial void OnSelectedAccountChanged(Account? value)
    {
        if (value is not null)
        {
            await RunAsync(() => LoadCardsAsync(value.Id));
        }
    }

    private async Task LoadCardsAsync(Guid accountId)
    {
        var cards = await _api.GetAccountCardsAsync(accountId);
        Cards.Clear();
        foreach (var card in cards)
        {
            Cards.Add(card);
        }
    }

    private bool CanIssueCard() =>
        !IsBusy && SelectedAccount is not null
        && decimal.TryParse(IssueSpendingLimitText, NumberStyles.Number, CultureInfo.InvariantCulture, out var limit) && limit > 0;

    [RelayCommand(CanExecute = nameof(CanIssueCard))]
    private Task IssueCardAsync() => RunAsync(async () =>
    {
        var limit = decimal.Parse(IssueSpendingLimitText, NumberStyles.Number, CultureInfo.InvariantCulture);
        var card = await _api.IssueCardAsync(new IssueCardRequest
        {
            AccountId = SelectedAccount!.Id,
            Type = IssueCardType,
            SpendingLimit = limit,
        });
        Cards.Add(card);
    });

    [RelayCommand]
    private Task FreezeAsync(Card card) => RunAsync(async () => ReplaceCard(await _api.FreezeCardAsync(card.Id)));

    [RelayCommand]
    private Task UnfreezeAsync(Card card) => RunAsync(async () => ReplaceCard(await _api.UnfreezeCardAsync(card.Id)));

    [RelayCommand]
    private void SelectForLimitEdit(Card card)
    {
        SelectedCard = card;
        EditSpendingLimitText = card.SpendingLimit.ToString("0.##", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private Task SaveSpendingLimitAsync() => RunAsync(async () =>
    {
        if (SelectedCard is null || !decimal.TryParse(EditSpendingLimitText, NumberStyles.Number, CultureInfo.InvariantCulture, out var limit) || limit <= 0)
        {
            return;
        }

        var updated = await _api.UpdateSpendingLimitAsync(SelectedCard.Id, new UpdateSpendingLimitRequest { SpendingLimit = limit });
        ReplaceCard(updated);
        SelectedCard = null;
    });

    [RelayCommand]
    private void CancelLimitEdit() => SelectedCard = null;

    private void ReplaceCard(Card updated)
    {
        var index = Cards.ToList().FindIndex(c => c.Id == updated.Id);
        if (index >= 0)
        {
            Cards[index] = updated;
        }
    }

    partial void OnIssueSpendingLimitTextChanged(string value) => IssueCardCommand.NotifyCanExecuteChanged();
}
