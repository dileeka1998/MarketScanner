namespace MarketScanner.Models;

/// <summary>
/// Represents a snapshot of scanner results.
/// </summary>
public sealed record ScannerSnapshot(
    IReadOnlyList<ScannerItem> Items
);
