using MarketScanner.Models;

namespace MarketScanner.Services;

/// <summary>
/// Interface for market scanner operations.
/// </summary>
public interface IScanner
{
    /// <summary>
    /// Event fired when a scanner snapshot is available.
    /// </summary>
    event Func<ScannerSnapshot, Task>? SnapshotReceived;
    Task<IReadOnlyList<ScannerRow>> ScanAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts scanning with the specified filters.
    /// </summary>
    Task StartAsync(
        FilterState filters,
        CancellationToken cancellationToken,
        Guid sessionId,
        Func<TickData, Task> onTick,
        Func<EnrichmentData, Task> onEnrichment);

    /// <summary>
    /// Stops the current scan operation.
    /// </summary>
    Task StopAsync();
}
