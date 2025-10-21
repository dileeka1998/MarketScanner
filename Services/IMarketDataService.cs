using MarketScanner.Models;

namespace MarketScanner.Services;

public interface IMarketDataService
{
    Task<IReadOnlyList<SnapshotRow>> GetSnapshotsAsync(UniverseRequest request, CancellationToken ct);
}
