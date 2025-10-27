using System.Globalization;

namespace MarketScanner.Converters;

public class IsSelectedWatchlistConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] == null || values[1] == null)
            return GetDefaultValue(targetType, parameter);
        
        if (values[0] is Models.Watchlist current && values[1] is Models.Watchlist selected)
        {
            bool isSelected = current.Id == selected.Id;
            
            // parameter tells us which property to return (stroke, background, etc.)
            if (parameter is string prop)
            {
                if (prop == "Stroke")
                    return isSelected ? Application.Current?.Resources["PrimaryDark"] ?? Colors.Blue : Application.Current?.Resources["BorderDark"] ?? Colors.Gray;
                if (prop == "Background")
                    return isSelected ? Application.Current?.Resources["PrimaryDark"] ?? Colors.Blue : Application.Current?.Resources["SurfaceVariantDark"] ?? Colors.DarkGray;
                if (prop == "StrokeThickness")
                    return isSelected ? 2 : 1;
            }
        }
        
        return GetDefaultValue(targetType, parameter);
    }

    private object? GetDefaultValue(Type targetType, object? parameter)
    {
        if (targetType == typeof(Color))
            return Colors.Transparent;
        if (targetType == typeof(Brush))
            return new SolidColorBrush(Colors.Transparent);
        if (targetType == typeof(int) || targetType == typeof(double))
            return 1;
        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

