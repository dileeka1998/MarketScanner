using MarketScanner.Views;

namespace MarketScanner
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        public AppShell(ContentPage contentPage) : this()
        {
            // Set the content page as the main content
            Items.Clear();
            Items.Add(new ShellContent
            {
                Title = "Market Scanner",
                Content = contentPage,
                Route = "MainPage"
            });
        }
    }
}
