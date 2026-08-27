using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Auth;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Maui.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;
    private readonly OidcAuthenticator _authenticator;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private Customer? customer;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string phoneNumber = string.Empty;
    [ObservableProperty] private string street = string.Empty;
    [ObservableProperty] private string city = string.Empty;
    [ObservableProperty] private string state = string.Empty;
    [ObservableProperty] private string zipCode = string.Empty;
    [ObservableProperty] private string country = string.Empty;

    [ObservableProperty]
    private bool showSavedConfirmation;

    public ProfileViewModel(AtlasApiClient api, OidcAuthenticator authenticator, INavigationService navigation)
    {
        _api = api;
        _authenticator = authenticator;
        _navigation = navigation;
    }

    public Task LoadAsync() => RunAsync(async () =>
    {
        Customer = await _api.GetMyProfileAsync();
        ResetEditFields(Customer);
    });

    private void ResetEditFields(Customer customer)
    {
        FirstName = customer.FirstName;
        LastName = customer.LastName;
        PhoneNumber = customer.PhoneNumber;
        Street = customer.Address.Street;
        City = customer.Address.City;
        State = customer.Address.State;
        ZipCode = customer.Address.ZipCode;
        Country = customer.Address.Country;
    }

    [RelayCommand]
    private void BeginEdit()
    {
        if (Customer is not null)
        {
            ResetEditFields(Customer);
        }
        ShowSavedConfirmation = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        Customer = await _api.UpdateMyProfileAsync(new UpdateCustomerRequest
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            PhoneNumber = PhoneNumber.Trim(),
            Address = new Address
            {
                Street = Street.Trim(),
                City = City.Trim(),
                State = State.Trim(),
                ZipCode = ZipCode.Trim(),
                Country = Country.Trim(),
            },
        });
        IsEditing = false;
        ShowSavedConfirmation = true;
    });

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _authenticator.SignOutAsync();
        await _navigation.GoToRootAsync(Routes.Login);
    }
}
