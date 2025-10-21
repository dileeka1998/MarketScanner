using Microsoft.Maui.Controls;

namespace MarketScanner.Views;

public partial class TestPage : ContentPage
{
    public TestPage()
    {
        InitializeComponent();
    }

    private void OnButtonClicked(object sender, EventArgs e)
    {
        DisplayAlert("Test", "Button clicked!", "OK");
    }
}
