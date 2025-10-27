using Microsoft.Extensions.Logging;

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WatchlistContentView: InitializeComponent() FAILED: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}

