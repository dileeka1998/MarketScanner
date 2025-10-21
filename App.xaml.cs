using MarketScanner.Views;
using MarketScanner.ViewModels;
using MarketScanner.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketScanner
{
    public partial class App : Application
    {
        public App(IServiceProvider services)
        {
            InitializeComponent();
            
            // Resolve scanner page from DI container
            var viewModel = services.GetRequiredService<ScannerViewModel>();
            var layout = services.GetRequiredService<ColumnLayoutService>();
            var scannerPage = new ScannerPage(viewModel, layout);
            
            MainPage = new AppShell(scannerPage);
        }
    }
}
