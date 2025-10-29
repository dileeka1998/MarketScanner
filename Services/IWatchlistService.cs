using MarketScanner.Models;

namespace MarketScanner.Services;

public interface IWatchlistService
{
    Task InitializeAsync();
    Task<List<Watchlist>> GetAllWatchlistsAsync();
    Task<Watchlist> CreateWatchlistAsync(string name);
    Task<bool> RenameWatchlistAsync(int watchlistId, string newName);
    Task<bool> DeleteWatchlistAsync(int watchlistId);
    Task<List<WatchlistItem>> GetWatchlistItemsAsync(int watchlistId);
    Task AddItemsAsync(int watchlistId, List<(string Symbol, string Company)> symbolsAndCompanies);
    Task RemoveItemAsync(int watchlistId, string symbol);
    Task ReorderItemsAsync(int watchlistId, List<string> orderedSymbols);
}

