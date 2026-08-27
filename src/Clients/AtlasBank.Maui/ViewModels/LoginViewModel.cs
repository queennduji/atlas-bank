using AtlasBank.Clients.Core.Auth;
using AtlasBank.Maui.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly OidcAuthenticator _authenticator;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private bool isRestoringSession = true;

    public LoginViewModel(OidcAuthenticator authenticator, INavigationService navigation)
    {
        _authenticator = authenticator;
        _navigation = navigation;
    }

    /// <summary>Called from LoginPage.OnAppearing — if there's a saved session from last time
    /// and it's still (or can be) valid, skip straight past the login screen instead of
    /// making the user tap "Sign in" every time.</summary>
    public async Task CheckForExistingSessionAsync()
    {
        IsRestoringSession = true;
        try
        {
            var session = await _authenticator.TryRestoreSessionAsync();
            if (session is not null)
            {
                await _navigation.GoToRootAsync(Routes.TabRoute(Routes.Dashboard));
            }
        }
        finally
        {
            IsRestoringSession = false;
        }
    }

    [RelayCommand]
    private Task SignInAsync() => RunAsync(async () =>
    {
        await _authenticator.SignInAsync();
        await _navigation.GoToRootAsync(Routes.TabRoute(Routes.Dashboard));
    });

    [RelayCommand]
    private Task GoToRegisterAsync() => _navigation.GoToAsync(Routes.Register);
}
