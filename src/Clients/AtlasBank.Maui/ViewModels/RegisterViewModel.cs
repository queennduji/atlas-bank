using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Maui.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtlasBank.Maui.ViewModels;

/// <summary>
/// Creates a customer via the public /api/customers/register route. Separate from Keycloak
/// sign-in — CustomerService provisions the Keycloak user itself as part of creating the
/// customer record. Field set matches frontend/src/pages/Register.tsx.
/// </summary>
public partial class RegisterViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string phoneNumber = string.Empty;
    [ObservableProperty] private DateTime dateOfBirth = DateTime.Today.AddYears(-18);
    [ObservableProperty] private string street = string.Empty;
    [ObservableProperty] private string city = string.Empty;
    [ObservableProperty] private string state = string.Empty;
    [ObservableProperty] private string zipCode = string.Empty;
    [ObservableProperty] private string country = string.Empty;
    [ObservableProperty] private bool isComplete;

    public RegisterViewModel(AtlasApiClient api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    private bool CanSubmit() =>
        !IsBusy
        && new[] { FirstName, LastName, Email, PhoneNumber, Street, City, State, ZipCode, Country }.All(v => !string.IsNullOrWhiteSpace(v))
        && Password.Length >= 8
        && Email.Contains('@');

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private Task SubmitAsync() => RunAsync(async () =>
    {
        await _api.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Email = Email.Trim(),
            Password = Password,
            PhoneNumber = PhoneNumber.Trim(),
            DateOfBirth = DateOfBirth.ToString("yyyy-MM-dd"),
            Address = new Address
            {
                Street = Street.Trim(),
                City = City.Trim(),
                State = State.Trim(),
                ZipCode = ZipCode.Trim(),
                Country = Country.Trim(),
            },
        });

        IsComplete = true;
    });

    [RelayCommand]
    private Task GoToLoginAsync() => _navigation.GoBackAsync();

    partial void OnFirstNameChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnLastNameChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnEmailChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnPhoneNumberChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnStreetChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnCityChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnStateChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnZipCodeChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnCountryChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
}
