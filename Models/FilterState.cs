namespace MarketScanner.Models;

/// <summary>
/// Represents the current state of all filters.
/// </summary>
public sealed record FilterState(
    string Region,
    string Product,
    string Sector,
    string Exchange,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinChangePercent,
    decimal? MaxChangePercent,
    decimal? MinRelVolume,
    int? VolumeMin,
    int TopN
)
{
    public static FilterState FromFilters(ScannerFilters filters) => new(
        filters.Region,
        filters.Product,
        filters.Sector,
        filters.Exchange,
        filters.MinPrice,
        filters.MaxPrice,
        filters.MinChangePercent,
        filters.MaxChangePercent,
        filters.MinRelVolume,
        filters.VolumeMin,
        filters.TopN
    );
}
