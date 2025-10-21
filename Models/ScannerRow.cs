namespace MarketScanner.Models;

public sealed class ScannerRow
{
    public int ReqId { get; init; }
    public string Symbol { get; init; } = "";
    public string Company { get; init; } = "";
    public decimal LastPrice { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePct { get; init; }
    public decimal RelativeVolume { get; init; } // RV
    public long Volume { get; init; }
    public long AvgVolume { get; init; }
    public decimal Float { get; init; }
    public decimal High52W { get; init; }
    public InstrumentMetadata Meta { get; init; } = new();
}
