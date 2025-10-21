namespace MarketScanner.Utils;

/// <summary>
/// Value conversion utilities for IBKR data
/// </summary>
public static class ValueConverters
{
    public static decimal? ToDecimal(object? value)
    {
        if (value == null) return null;
        
        if (value is decimal d) return d;
        if (value is double db) return (decimal)db;
        if (value is float f) return (decimal)f;
        if (value is int i) return (decimal)i;
        if (value is long l) return (decimal)l;
        
        if (decimal.TryParse(value.ToString(), out var result))
            return result;
            
        return null;
    }

    public static long? ToLong(object? value)
    {
        if (value == null) return null;
        
        if (value is long l) return l;
        if (value is int i) return (long)i;
        if (value is decimal d) return (long)d;
        if (value is double db) return (long)db;
        if (value is float f) return (long)f;
        
        if (long.TryParse(value.ToString(), out var result))
            return result;
            
        return null;
    }

    public static double? ToDouble(object? value)
    {
        if (value == null) return null;
        
        if (value is double d) return d;
        if (value is decimal dec) return (double)dec;
        if (value is float f) return (double)f;
        if (value is int i) return (double)i;
        if (value is long l) return (double)l;
        
        if (double.TryParse(value.ToString(), out var result))
            return result;
            
        return null;
    }

    public static int? ToInt(object? value)
    {
        if (value == null) return null;
        
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is decimal d) return (int)d;
        if (value is double db) return (int)db;
        if (value is float f) return (int)f;
        
        if (int.TryParse(value.ToString(), out var result))
            return result;
            
        return null;
    }
}
