namespace MarketScanner.Models;

/// <summary>
/// Represents the status of a scanner row.
/// </summary>
public enum RowStatus
{
    PendingEnrichment,
    Enriched,
    Error,
    Skipped
}

/// <summary>
/// Represents the fundamentals status of a scanner row.
/// </summary>
public enum FundamentalsStatus
{
    Pending,
    Loading,
    Loaded,
    Error,
    Skipped
}
