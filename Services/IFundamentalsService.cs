using MarketScanner.Models;

namespace MarketScanner.Services;

public interface IFundamentalsService
{
    Task<decimal?> GetFloatSharesAsync(string symbol, CancellationToken cancellationToken = default);
    Task<decimal?> GetWeek52HighAsync(string symbol, CancellationToken cancellationToken = default);
}
