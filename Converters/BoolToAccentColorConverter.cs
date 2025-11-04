using System.Globalization;

namespace MarketScanner.Converters;

public class BoolToAccentColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool isSelected && isSelected)
            ? Color.FromArgb("#1e88e5")  // Accent color when selected
            : Colors.Transparent;         // Transparent when not selected
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

