using MarketScanner.ViewModels;

namespace MarketScanner.Views;

public partial class WatchlistWindow : ContentPage
{
    public WatchlistWindow(WatchlistViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is WatchlistViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is WatchlistViewModel vm)
        {
            vm.Dispose();
        }
    }
}

