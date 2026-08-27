using AtlasBank.Maui.ViewModels;

namespace AtlasBank.Maui.Views;

public partial class StatementsPage : ContentPage
{
    private readonly StatementsViewModel _viewModel;

    public StatementsPage(StatementsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
