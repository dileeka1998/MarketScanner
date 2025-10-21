using System.Globalization;

namespace MarketScanner.Converters;

public class RefreshIntervalToTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentInterval && parameter is string paramStr && int.TryParse(paramStr, out int targetInterval))
        {
            return currentInterval == targetInterval ? Color.FromArgb("#ffffff") : Color.FromArgb("#e0e0e0"); // OnSurfaceDark : OnSurfaceVariantDark
        }
        return Color.FromArgb("#e0e0e0"); // OnSurfaceVariantDark
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
