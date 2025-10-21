// MarketScanner/Models/UniverseRequest.cs
namespace MarketScanner.Models;

public sealed class UniverseRequest
{
    // optional: comma/space-separated symbols from the UI text box
    public string? SymbolsCsv { get; init; }

    // filters (all optional)
    public string Region { get; init; } = "United States";   // maps to STK.US.*
    public string Product { get; init; } = "Stocks";          // STK
    public string Exchange { get; init; } = "US Stocks";      // MAJOR
    public string Sector { get; init; } = "Any";

    public int? TopN { get; init; } = 25;

    public double? PriceMin { get; init; }
    public double? PriceMax { get; init; }

    public double? ChangePctMin { get; init; }
    public double? ChangePctMax { get; init; }

    // Relative volume – we'll map to IBKR's avgVolumeAbove/volumeAbove pair
    public double? RelativeVolumeMin { get; init; }
    
    // Volume minimum
    public int? VolumeMin { get; init; }

    // convenience: parsed symbols
    public IReadOnlyList<string> ParseSymbols() =>
        string.IsNullOrWhiteSpace(SymbolsCsv)
            ? Array.Empty<string>()
            : SymbolsCsv.Split(new[] {',',' ',';','\t','\n','\r'}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToUpperInvariant())
                        .Distinct()
                        .ToArray();
}
