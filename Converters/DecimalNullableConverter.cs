using System.Globalization;

namespace MarketScanner.Converters;

public sealed class DecimalNullableConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is decimal d ? d.ToString(culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s)) return null!;
        return decimal.TryParse(s, NumberStyles.Any, culture, out var d) ? d : null!;
    }
}