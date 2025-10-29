using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MarketScanner.Models;
using MarketScanner.ViewModels;
using Microsoft.Extensions.Logging;


namespace MarketScanner.Core
{
    public static class FilterEngine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Matches(string? rowValue, string? criterion)
        {
            if (string.IsNullOrWhiteSpace(criterion)) return true;
            if (string.IsNullOrWhiteSpace(rowValue)) return false;
            return string.Equals(rowValue.Trim(), criterion.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public sealed record Criteria(
            string? Exchange,  // Region, Product, and RelativeVolume removed
            decimal? MinPrice, decimal? MaxPrice,
            decimal? MinChgPct,
            long? MinVolume,
            int TopN);
        public sealed record Result(int[] TopIndices);

        public static Result Apply(ScannerRowViewModel[] rows, Criteria c, ILogger? logger = null)
        {
            // filter
            var idx = Enumerable.Range(0, rows.Length).Where(i =>
            {
                var r = rows[i];

                // Allow items with pending price/volume data (will update within 1-2 seconds)
                // But still apply change% filter since ChangePercent is calculated from live data
                bool hasPendingPriceVolume = r.LastPrice == 0 || r.Volume == 0;

                // Apply metadata filters
                if (!Matches(r.Exchange, c.Exchange)) return false;

                // Price/volume filters - skip if data is pending
                if (!hasPendingPriceVolume)
                {
                    if (c.MinPrice is { } pmin && r.LastPrice < (double)pmin) return false;
                    if (c.MaxPrice is { } pmax && r.LastPrice > (double)pmax) return false;
                    if (c.MinVolume is { } vmin && r.Volume < vmin) return false;
                }

                // User-defined filters - ALWAYS apply (uses live tick data)
                // MinChgPct comparison: both values are percentages (e.g., 9.0 means 9%)
                if (c.MinChgPct is { } cmin)
                {
                    logger?.LogInformation("Comparing {Symbol}: MinChgPct={MinChgPct} vs ChangePercent={ChangePercent} => Pass={Pass}", 
                        r.Symbol, c.MinChgPct, r.ChangePercent, r.ChangePercent >= (double)cmin);
                    if (r.ChangePercent < (double)cmin) return false;
                }

                return true;
            });

            // default sort: Change % desc
            var ordered = idx.OrderByDescending(i => rows[i].ChangePercent);

            // TopN (guard)
            var top = (c.TopN > 0 ? ordered.Take(c.TopN) : ordered).ToArray();
            return new Result(top);
        }
    }
}
