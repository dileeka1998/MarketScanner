using MarketScanner.ViewModels;

namespace MarketScanner.Models;

/// <summary>
/// Represents a market data tick.
/// </summary>
public sealed record TickData(
    string Symbol,
    Guid SessionId,
    double? LastPrice,
    double? ClosePrice,
    long? Volume,
    double? FiftyTwoWeekHigh,
    DateTime Timestamp,
    double? Bid = null,
    double? Ask = null,
    double? High = null,
    double? Low = null,
    double? Open = null,
    double? PreviousClose = null,
    long? AverageVolume = null,
    decimal? RelativeVolume = null,
    decimal? Change = null,
    decimal? ChangePercent = null
)
{
    public ScannerRowViewModel ToRow(string region, string product, string exchange) => new()
    {
        Symbol = Symbol,
        Company = Symbol, // Use symbol as company name
        Region = region,
        Product = product,
        Exchange = exchange
    };

    public void ApplyTo(ScannerRowViewModel row)
    {
        if (LastPrice.HasValue) row.UpdateLastPrice(LastPrice.Value);
        if (ClosePrice.HasValue) row.UpdateClosePrice(ClosePrice.Value);
        if (Volume.HasValue) row.UpdateVolume(Volume.Value);
        if (AverageVolume.HasValue) row.SetAvgVolume(AverageVolume.Value);
    }
}
