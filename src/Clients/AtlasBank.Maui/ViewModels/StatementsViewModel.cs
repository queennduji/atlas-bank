using System.Collections.ObjectModel;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Maui.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

[QueryProperty(nameof(PreselectedAccountId), "accountId")]
public partial class StatementsViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;
    private readonly INavigationService _navigation;

    public ObservableCollection<Account> Accounts { get; } = [];
    public ObservableCollection<StatementSummary> Statements { get; } = [];

    [ObservableProperty]
    private Guid preselectedAccountId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    private Account? selectedAccount;

    [ObservableProperty]
    private DateTime periodStart = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime periodEnd = DateTime.Today;

    public bool HasStatements => Statements.Count > 0;

    public StatementsViewModel(AtlasApiClient api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
        Statements.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasStatements));
    }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var accounts = await _api.GetMyAccountsAsync();
        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(account);
        }

        SelectedAccount = (PreselectedAccountId != Guid.Empty ? Accounts.FirstOrDefault(a => a.Id == PreselectedAccountId) : null)
            ?? SelectedAccount
            ?? Accounts.FirstOrDefault();

        if (SelectedAccount is not null)
        {
            await LoadStatementsAsync(SelectedAccount.Id);
        }
    });

    async partial void OnSelectedAccountChanged(Account? value)
    {
        if (value is not null)
        {
            await RunAsync(() => LoadStatementsAsync(value.Id));
        }
    }

    private async Task LoadStatementsAsync(Guid accountId)
    {
        var statements = await _api.GetAccountStatementsAsync(accountId);
        Statements.Clear();
        foreach (var statement in statements.OrderByDescending(s => s.PeriodEnd))
        {
            Statements.Add(statement);
        }
    }

    private bool CanGenerate() => !IsBusy && SelectedAccount is not null && PeriodStart < PeriodEnd;

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private Task GenerateAsync() => RunAsync(async () =>
    {
        var statement = await _api.GenerateStatementAsync(new GenerateStatementRequest
        {
            AccountId = SelectedAccount!.Id,
            PeriodStart = DateOnly.FromDateTime(PeriodStart),
            PeriodEnd = DateOnly.FromDateTime(PeriodEnd),
        });

        await LoadStatementsAsync(SelectedAccount.Id);
        await ViewStatementAsync(new StatementSummary
        {
            Id = statement.Id,
            AccountId = statement.AccountId,
            AccountNumber = statement.AccountNumber,
            PeriodStart = statement.PeriodStart,
            PeriodEnd = statement.PeriodEnd,
            ClosingBalance = statement.ClosingBalance,
            GeneratedAt = statement.GeneratedAt,
        });
    });

    [RelayCommand]
    private Task ViewStatementAsync(StatementSummary statement) =>
        _navigation.GoToAsync(Routes.StatementDetail, new Dictionary<string, object> { ["statementId"] = statement.Id });
}
