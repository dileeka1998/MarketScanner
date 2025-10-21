using System.Text.Json;

namespace MarketScanner.Extensions;

public static class PreferencesExtensions
{
    public static void SetDoubleArray(string key, double[] array)
    {
        try
        {
            var json = JsonSerializer.Serialize(array);
            Preferences.Set(key, json);
        }
        catch
        {
            // Ignore errors
        }
    }

    public static double[] GetDoubleArray(string key, double[] defaultArray)
    {
        try
        {
            var json = Preferences.Get(key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var result = JsonSerializer.Deserialize<double[]>(json);
                if (result != null && result.Length == defaultArray.Length)
                {
                    return result;
                }
            }
        }
        catch
        {
            // Return default on error
        }
        
        return defaultArray;
    }
}