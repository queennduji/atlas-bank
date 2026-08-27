using AtlasBank.Maui.ViewModels;

namespace AtlasBank.Maui.Views;

public partial class TransferPage : ContentPage
{
    public TransferPage(TransferViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
