using System;
using System.Globalization;

namespace MarketScanner.Utilities
{
    public static class Parsing
    {
        public static decimal? ParsePercent(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim()
                     .Replace("%", "", StringComparison.Ordinal)
                     .Replace("+", "", StringComparison.Ordinal);
            if (decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d; // Stored model uses percent value (not 0-1)
            return null;
        }

        public static decimal? ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        }
    }
}
