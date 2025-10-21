using System.Globalization;
using Microsoft.Maui.Controls;

namespace MarketScanner.Converters
{
    public sealed class NullableDecimalConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value;

        // "": null, "  " : null, invalid: Binding.DoNothing
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = (value?.ToString() ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return null;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            return Binding.DoNothing;
        }
    }
}
