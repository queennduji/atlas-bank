using AtlasBank.Maui.ViewModels;

namespace AtlasBank.Maui.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        DobPicker.MaximumDate = DateTime.Today;
    }
}
