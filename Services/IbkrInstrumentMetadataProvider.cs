using MarketScanner.Models;
using Microsoft.Extensions.Logging;

namespace MarketScanner.Services;

/// <summary>
/// Simple IBKR instrument metadata provider
/// </summary>
public sealed class IbkrInstrumentMetadataProvider : IInstrumentMetadataProvider
{
    private readonly ILogger<IbkrInstrumentMetadataProvider> _logger;
    private readonly Dictionary<string, InstrumentMetadata> _cache = new();

    public IbkrInstrumentMetadataProvider(ILogger<IbkrInstrumentMetadataProvider> logger)
    {
        _logger = logger;
    }

    public Task<InstrumentMetadata?> GetMetadataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(symbol, out var cached))
        {
            return Task.FromResult<InstrumentMetadata?>(cached);
        }

        // Generate mock metadata for now
        var metadata = new InstrumentMetadata
        {
            Symbol = symbol,
            Company = GetCompanyName(symbol),
            Sector = GetSector(symbol),
            Exchange = GetExchange(symbol),
            Region = "United States",
            Product = "Stocks",
            MarketCap = GetMarketCap(symbol),
            FloatShares = GetFloatShares(symbol),
            FiftyTwoWeekHigh = GetFiftyTwoWeekHigh(symbol),
            FiftyTwoWeekLow = GetFiftyTwoWeekLow(symbol),
            LastUpdated = DateTime.UtcNow
        };

        _cache[symbol] = metadata;
        return Task.FromResult<InstrumentMetadata?>(metadata);
    }

    public async Task<IReadOnlyDictionary<string, InstrumentMetadata>> GetMetadataAsync(
        IEnumerable<string> symbols, 
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, InstrumentMetadata>();
        
        foreach (var symbol in symbols)
        {
            var metadata = await GetMetadataAsync(symbol, cancellationToken);
            if (metadata != null)
            {
                result[symbol] = metadata;
            }
        }

        return result;
    }

    private static string GetCompanyName(string symbol)
    {
        return symbol switch
        {
            "AAPL" => "Apple Inc.",
            "MSFT" => "Microsoft Corporation",
            "NVDA" => "NVIDIA Corporation",
            "AMZN" => "Amazon.com Inc.",
            "META" => "Meta Platforms Inc.",
            "GOOGL" => "Alphabet Inc.",
            "TSLA" => "Tesla Inc.",
            "AMD" => "Advanced Micro Devices Inc.",
            "NFLX" => "Netflix Inc.",
            "SPY" => "SPDR S&P 500 ETF Trust",
            _ => symbol
        };
    }

    private static string GetSector(string symbol)
    {
        return symbol switch
        {
            "AAPL" or "MSFT" or "NVDA" or "AMD" or "GOOGL" or "META" or "NFLX" => "Technology",
            "AMZN" => "Consumer Discretionary",
            "TSLA" => "Consumer Discretionary",
            "SPY" => "Diversified",
            _ => "Technology"
        };
    }

    private static string GetExchange(string symbol)
    {
        return symbol switch
        {
            "AAPL" or "MSFT" or "NVDA" or "AMZN" or "META" or "GOOGL" or "TSLA" or "AMD" or "NFLX" => "NASDAQ",
            "SPY" => "NYSE",
            _ => "NASDAQ"
        };
    }

    private static decimal? GetMarketCap(string symbol)
    {
        var random = new Random(symbol.GetHashCode());
        return (decimal)(random.NextDouble() * 2_000_000_000_000 + 100_000_000_000); // 100B to 2T
    }

    private static decimal? GetFloatShares(string symbol)
    {
        var random = new Random(symbol.GetHashCode());
        return (decimal)(random.NextDouble() * 1000 + 100); // 100M to 1.1B shares
    }

    private static decimal? GetFiftyTwoWeekHigh(string symbol)
    {
        var random = new Random(symbol.GetHashCode());
        return (decimal)(random.NextDouble() * 200 + 50); // $50 to $250
    }

    private static decimal? GetFiftyTwoWeekLow(string symbol)
    {
        var random = new Random(symbol.GetHashCode());
        return (decimal)(random.NextDouble() * 50 + 10); // $10 to $60
    }
}
