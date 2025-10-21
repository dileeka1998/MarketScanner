namespace MarketScanner.Models;

/// <summary>
/// Represents metadata for an instrument.
/// </summary>
public sealed class InstrumentMetadata
{
    public string Symbol { get; init; } = string.Empty;
    public string? Company { get; init; }
    public string? Sector { get; init; }
    public string? Exchange { get; init; }
    public string? Region { get; init; }
    public string? Product { get; init; }
    public decimal? MarketCap { get; init; }
    public decimal? FloatShares { get; init; }
    public decimal? FiftyTwoWeekHigh { get; init; }
    public decimal? FiftyTwoWeekLow { get; init; }
    public DateTime? LastUpdated { get; init; }
}
