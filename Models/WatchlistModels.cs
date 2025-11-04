using SQLite;

namespace MarketScanner.Models;

[Table("watchlists")]
public class Watchlist
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100), NotNull]
    public string Name { get; set; } = string.Empty;

    [NotNull]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotNull]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("watchlist_items")]
public class WatchlistItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, NotNull]
    public int WatchlistId { get; set; }

    [MaxLength(20), NotNull, Indexed]
    public string Symbol { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Company { get; set; }

    [NotNull]
    public int DisplayOrder { get; set; }

    [NotNull]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

