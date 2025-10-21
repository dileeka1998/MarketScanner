using System.Collections.Immutable;

namespace MarketScanner.Core;

public sealed class Snapshot
{
    public readonly int Version;
    public readonly int Count;

    // Row objects for materialization (immutable array of references or records)
    public readonly ImmutableArray<Models.ScannerItem> Rows;

    // Columnar hot fields (aligned with Rows by index)
    public readonly string[] Symbol;
    public readonly string?[] Company;
    public readonly string[] Region;
    public readonly string[] Product;
    public readonly string[] Sector;
    public readonly string[] Exchange;

    public readonly decimal[] LastPrice;
    public readonly decimal[] Change;
    public readonly decimal[] ChangePct;
    public readonly decimal[] RelVol;        // precomputed (Volume/AvgVol if missing)
    public readonly long[] Volume;
    public readonly long[] AvgVol;
    public readonly decimal[] Float;
    public readonly decimal[] High52;

    public Snapshot(int version,
                    ImmutableArray<Models.ScannerItem> rows,
                    string[] symbol, string?[] company, string[] region, string[] product, string[] sector, string[] exchange,
                    decimal[] lastPrice, decimal[] change, decimal[] changePct, decimal[] relVol,
                    long[] volume, long[] avgVol, decimal[] @float, decimal[] high52)
    {
        Version = version;
        Rows = rows;
        Count = rows.Length;

        Symbol = symbol;
        Company = company;
        Region = region;
        Product = product;
        Sector = sector;
        Exchange = exchange;

        LastPrice = lastPrice;
        Change = change;
        ChangePct = changePct;
        RelVol = relVol;
        Volume = volume;
        AvgVol = avgVol;
        Float = @float;
        High52 = high52;
    }
}
