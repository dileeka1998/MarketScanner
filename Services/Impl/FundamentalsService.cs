using MarketScanner.Config;
using System.Text.Json;

namespace MarketScanner.Services.Impl;

public class FundamentalsService : IFundamentalsService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public FundamentalsService(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<decimal?> GetFloatSharesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);

            var response = await _httpClient.GetAsync($"{_settings.IbkrProxyBaseUrl}/fundamentals/{symbol}/float", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (data.TryGetProperty("floatShares", out var floatElement))
            {
                return floatElement.GetDecimal();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<decimal?> GetWeek52HighAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);

            var response = await _httpClient.GetAsync($"{_settings.IbkrProxyBaseUrl}/fundamentals/{symbol}/week52high", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (data.TryGetProperty("week52High", out var highElement))
            {
                return highElement.GetDecimal();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
