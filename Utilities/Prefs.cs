using Microsoft.Maui.Storage;

namespace MarketScanner.Utilities;

public static class Prefs
{
    public static int? GetInt(string key) => Preferences.ContainsKey(key) ? Preferences.Get(key, 0) : null;
    public static void SetInt(string key, int value) => Preferences.Set(key, value);
    public static void Remove(string key) { if (Preferences.ContainsKey(key)) Preferences.Remove(key); }
}
