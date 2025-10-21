using System.Globalization;

namespace MarketScanner.Converters;

public class RefreshIntervalToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentInterval && parameter is string paramStr && int.TryParse(paramStr, out int targetInterval))
        {
            return currentInterval == targetInterval ? Color.FromArgb("#4a9eff") : Color.FromArgb("#2d2d2d"); // PrimaryDark : SurfaceDark
        }
        return Color.FromArgb("#2d2d2d"); // SurfaceDark
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
