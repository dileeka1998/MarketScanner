using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarketScanner.Controls;

/// <summary>
/// Reusable auto-refresh control with checkbox and 30s/60s interval selection
/// </summary>
public partial class AutoRefreshControl : ContentView, INotifyPropertyChanged
{
    public static readonly BindableProperty AutoRefreshEnabledProperty =
        BindableProperty.Create(nameof(AutoRefreshEnabled), typeof(bool), typeof(AutoRefreshControl), false, BindingMode.TwoWay);

    public static readonly BindableProperty RefreshIntervalSecondsProperty =
        BindableProperty.Create(nameof(RefreshIntervalSeconds), typeof(int), typeof(AutoRefreshControl), 60, BindingMode.TwoWay);

    public bool AutoRefreshEnabled
    {
        get => (bool)GetValue(AutoRefreshEnabledProperty);
        set => SetValue(AutoRefreshEnabledProperty, value);
    }

    public int RefreshIntervalSeconds
    {
        get => (int)GetValue(RefreshIntervalSecondsProperty);
        set => SetValue(RefreshIntervalSecondsProperty, value);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    public AutoRefreshControl()
    {
        InitializeComponent();
    }

    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
