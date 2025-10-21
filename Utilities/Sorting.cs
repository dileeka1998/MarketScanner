using MarketScanner.Models;

namespace MarketScanner.Utilities;

public static class Sorting
{
    public static IEnumerable<ScannerItem> SortBy(this IEnumerable<ScannerItem> items, string sortBy, bool ascending = false)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "symbol" => ascending ? items.OrderBy(x => x.Symbol) : items.OrderByDescending(x => x.Symbol),
            "company" => ascending ? items.OrderBy(x => x.Company) : items.OrderByDescending(x => x.Company),
            "changepercent" => ascending ? items.OrderBy(x => x.ChangePercent) : items.OrderByDescending(x => x.ChangePercent),
            "change" => ascending ? items.OrderBy(x => x.Change) : items.OrderByDescending(x => x.Change),
            "lastprice" => ascending ? items.OrderBy(x => x.LastPrice) : items.OrderByDescending(x => x.LastPrice),
            "relativevolume" => ascending ? items.OrderBy(x => x.RelativeVolume) : items.OrderByDescending(x => x.RelativeVolume),
            "volume" => ascending ? items.OrderBy(x => x.Volume) : items.OrderByDescending(x => x.Volume),
            "averagevolume" => ascending ? items.OrderBy(x => x.AverageVolume) : items.OrderByDescending(x => x.AverageVolume),
            "float" => ascending ? items.OrderBy(x => x.Float) : items.OrderByDescending(x => x.Float),
            "week52high" => ascending ? items.OrderBy(x => x.FiftyTwoWeekHigh) : items.OrderByDescending(x => x.FiftyTwoWeekHigh),
            _ => items.OrderByDescending(x => x.ChangePercent)
        };
    }
}
