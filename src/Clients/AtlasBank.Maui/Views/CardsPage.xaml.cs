using AtlasBank.Maui.ViewModels;

namespace AtlasBank.Maui.Views;

public partial class CardsPage : ContentPage
{
    private readonly CardsViewModel _viewModel;

    public CardsPage(CardsViewModel viewModel)
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
