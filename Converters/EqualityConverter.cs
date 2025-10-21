using System.Globalization;

namespace MarketScanner.Converters;

public sealed class EqualityConverter : IValueConverter
{
    // Match IValueConverter: object? parameters
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int v && int.TryParse(parameter?.ToString(), out var p)) return v == p;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Only update when RadioButton becomes checked (true)
        if (value is bool b && b && int.TryParse(parameter?.ToString(), out var p)) return p;
        return Binding.DoNothing;
    }
}
