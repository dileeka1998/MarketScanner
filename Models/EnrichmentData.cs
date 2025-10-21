namespace MarketScanner.Models;

/// <summary>
/// Represents enrichment data for a symbol.
/// </summary>
public sealed record EnrichmentData(
    string Symbol,
    Guid SessionId,
    decimal? RelativeVolume,
    long? AvgVolume,
    decimal? FloatShares,
    decimal? FiftyTwoWeekHigh,
    DateTime Timestamp
);
