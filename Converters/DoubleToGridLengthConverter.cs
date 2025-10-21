using System.Globalization;

namespace MarketScanner.Converters;

public class DoubleToGridLengthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && !double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue))
        {
            return new GridLength(doubleValue, GridUnitType.Absolute);
        }
        
        return new GridLength(120, GridUnitType.Absolute);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength && gridLength.GridUnitType == GridUnitType.Absolute)
        {
            return gridLength.Value;
        }
        
        return 120.0;
    }
}