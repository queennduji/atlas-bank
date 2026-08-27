using AtlasBank.Maui.ViewModels;

namespace AtlasBank.Maui.Views;

public partial class StatementDetailPage : ContentPage
{
    public StatementDetailPage(StatementDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
