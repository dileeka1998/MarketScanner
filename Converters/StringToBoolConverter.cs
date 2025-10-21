using System.Globalization;

namespace MarketScanner.Converters;

public class StringToBoolConverter : IValueConverter
{
    public static StringToBoolConverter Instance { get; } = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
