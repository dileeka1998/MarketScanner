namespace MarketScanner.Models;

/// <summary>
/// Represents a row in a market data snapshot.
/// </summary>
public sealed class SnapshotRow
{
    public string Symbol { get; init; } = "";
    public string? Company { get; init; }
    public decimal LastPrice { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePercent { get; init; }
    public decimal RelativeVolume { get; init; }
    public long Volume { get; init; }
    public long AverageVolume { get; init; }
    public decimal Float { get; init; }
    public decimal FiftyTwoWeekHigh { get; init; }
    public string? Sector { get; init; }
    public string? Exchange { get; init; }
    public string? Region { get; init; }
    public string? Product { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
