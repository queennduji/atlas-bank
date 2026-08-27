using System.Collections.ObjectModel;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Maui.Services.Navigation;
using AtlasBank.Maui.Services.Offline;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private const string AccountsCacheKey = "accounts";

    private readonly AtlasApiClient _api;
    private readonly INavigationService _navigation;
    private readonly IOfflineCache _cache;

    public ObservableCollection<Account> Accounts { get; } = [];

    [ObservableProperty]
    private string? customerFirstName;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isShowingCachedData;

    [ObservableProperty]
    private DateTimeOffset? cachedAsOfUtc;

    [ObservableProperty]
    private AccountType newAccountType = AccountType.Checking;

    public IReadOnlyList<AccountType> AccountTypes { get; } = Enum.GetValues<AccountType>();

    public bool HasAccounts => Accounts.Count > 0;

    public DashboardViewModel(AtlasApiClient api, INavigationService navigation, IOfflineCache cache)
    {
        _api = api;
        _navigation = navigation;
        _cache = cache;
        Accounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAccounts));
    }

    public Task LoadAsync() => RunAsync(RefreshCoreAsync);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await RunAsync(RefreshCoreAsync);
        IsRefreshing = false;
    }

    private async Task RefreshCoreAsync()
    {
        try
        {
            var profile = await _api.GetMyProfileAsync();
            CustomerFirstName = profile.FirstName;

            var accounts = await _api.GetMyAccountsAsync();
            ReplaceAccounts(accounts);
            IsShowingCachedData = false;
            await _cache.SaveAsync(AccountsCacheKey, accounts);
        }
        catch (HttpRequestException) when (Accounts.Count == 0)
        {
            var cached = await _cache.LoadAsync<IReadOnlyList<Account>>(AccountsCacheKey);
            if (cached is null)
            {
                throw;
            }

            ReplaceAccounts(cached.Value.Value);
            IsShowingCachedData = true;
            CachedAsOfUtc = cached.Value.SavedAtUtc;
        }
    }

    private void ReplaceAccounts(IEnumerable<Account> accounts)
    {
        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(account);
        }
    }

    [RelayCommand]
    private Task OpenAccountAsync() => RunAsync(async () =>
    {
        var account = await _api.CreateAccountAsync(new CreateAccountRequest { Type = NewAccountType });
        Accounts.Add(account);
    });

    [RelayCommand]
    private Task ViewAccountAsync(Account account) =>
        _navigation.GoToAsync(Routes.AccountDetail, new Dictionary<string, object> { ["accountId"] = account.Id });
}
