using Microsoft.Extensions.Logging;
using MarketScanner.ViewModels;

namespace MarketScanner.Views;

public partial class WatchlistContentView : ContentView
{
    public WatchlistContentView()
    {
        try
        {
            Console.WriteLine("WatchlistContentView: InitializeComponent() starting");
            InitializeComponent();
            Console.WriteLine("WatchlistContentView: InitializeComponent() completed successfully");
            
            // Auto-focus entry when popup becomes visible
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BindingContext))
                {
                    if (BindingContext is WatchlistViewModel vm)
                    {
                        vm.PropertyChanged += (sender, args) =>
                        {
                            if (args.PropertyName == nameof(WatchlistViewModel.IsCreatingWatchlist) 
                                && vm.IsCreatingWatchlist)
                            {
                                Dispatcher.Dispatch(() =>
                                {
                                    WatchlistNameEntry?.Focus();
                                    if (WatchlistNameEntry != null)
                                    {
                                        WatchlistNameEntry.CursorPosition = 0;
                                        WatchlistNameEntry.SelectionLength = WatchlistNameEntry.Text?.Length ?? 0;
                                    }
                                });
                            }
                        };
                    }
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WatchlistContentView: InitializeComponent() FAILED: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}

