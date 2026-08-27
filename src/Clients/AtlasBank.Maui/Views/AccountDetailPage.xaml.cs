using AtlasBank.Maui.ViewModels;

namespace AtlasBank.Maui.Views;

public partial class AccountDetailPage : ContentPage
{
    public AccountDetailPage(AccountDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
