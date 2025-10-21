using Microsoft.Maui.Controls;

namespace MarketScanner.Views;

public partial class SimpleScannerPage : ContentPage
{
    private int _selectedInterval = 60; // Default to 60 seconds

    public SimpleScannerPage()
    {
        InitializeComponent();
        UpdateButtonStyles();
        UpdateLabels();
    }

    private void OnIntervalButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            if (button == Button30s)
            {
                _selectedInterval = 30;
            }
            else if (button == Button60s)
            {
                _selectedInterval = 60;
            }
            
            UpdateButtonStyles();
            UpdateLabels();
        }
    }

    private void UpdateButtonStyles()
    {
        // Update 30s button
        if (_selectedInterval == 30)
        {
            Button30s.BackgroundColor = Color.FromArgb("#4a9eff"); // Primary color
            Button30s.TextColor = Color.FromArgb("#ffffff"); // White text
        }
        else
        {
            Button30s.BackgroundColor = Color.FromArgb("#2d2d2d"); // Dark background
            Button30s.TextColor = Color.FromArgb("#e0e0e0"); // Light text
        }

        // Update 60s button
        if (_selectedInterval == 60)
        {
            Button60s.BackgroundColor = Color.FromArgb("#4a9eff"); // Primary color
            Button60s.TextColor = Color.FromArgb("#ffffff"); // White text
        }
        else
        {
            Button60s.BackgroundColor = Color.FromArgb("#2d2d2d"); // Dark background
            Button60s.TextColor = Color.FromArgb("#e0e0e0"); // Light text
        }
    }

    private void UpdateLabels()
    {
        IntervalLabel.Text = $"Selected Interval: {_selectedInterval} seconds";
        StatusLabel.Text = $"Auto-refresh: {(AutoRefreshCheckBox.IsChecked ? "ON" : "OFF")}";
    }
}
