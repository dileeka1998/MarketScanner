using System.Globalization;

namespace MarketScanner.Converters;

public class ChangeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double change = 0;
        
        if (value is double d)
            change = d;
        else if (value is decimal dec)
            change = (double)dec;
        else
            return Application.Current?.Resources["NeutralColor"] ?? Colors.Gray;
        
        if (change > 0)
            return Application.Current?.Resources["GainColor"] ?? Colors.Green;
        else if (change < 0)
            return Application.Current?.Resources["LossColor"] ?? Colors.Red;
        
        return Application.Current?.Resources["NeutralColor"] ?? Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
