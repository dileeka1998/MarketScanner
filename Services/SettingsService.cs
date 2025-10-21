using MarketScanner.Config;
using MarketScanner.Models;
using System.Text.Json;

namespace MarketScanner.Services;

public class SettingsService
{
    private readonly string _settingsKey = "scanner_filters";
    private readonly string _configKey = "app_config";

    public async Task<ScannerFilters> GetFiltersAsync()
    {
        try
        {
            var json = await SecureStorage.GetAsync(_settingsKey);
            if (string.IsNullOrEmpty(json))
                return new ScannerFilters();

            return JsonSerializer.Deserialize<ScannerFilters>(json) ?? new ScannerFilters();
        }
        catch
        {
            return new ScannerFilters();
        }
    }

    public async Task SaveFiltersAsync(ScannerFilters filters)
    {
        try
        {
            var json = JsonSerializer.Serialize(filters);
            await SecureStorage.SetAsync(_settingsKey, json);
        }
        catch
        {
            // Log error but don't throw
        }
    }

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        try
        {
            var json = await SecureStorage.GetAsync(_configKey);
            if (string.IsNullOrEmpty(json))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            await SecureStorage.SetAsync(_configKey, json);
        }
        catch
        {
            // Log error but don't throw
        }
    }
}
