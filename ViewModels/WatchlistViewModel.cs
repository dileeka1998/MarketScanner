using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketScanner.Models;
using MarketScanner.Services;
using MarketScanner.Services.Ibkr;
using MarketScanner.Views.Dialogs;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;

namespace MarketScanner.ViewModels;

public partial class WatchlistViewModel : ObservableObject, IDisposable
{
    private readonly IWatchlistService _watchlistService;
    private readonly IbkrGatewayService _ibkrService;
    private readonly IDispatcherService _dispatcher;
    private readonly ILogger<WatchlistViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Watchlist> _watchlists = new();
    [ObservableProperty] private Watchlist? _selectedWatchlist;
    [ObservableProperty] private ObservableCollection<ScannerRowViewModel> _watchlistItems = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    
    // Inline watchlist creation popup
    [ObservableProperty] private bool _isCreatingWatchlist = false;
    [ObservableProperty] private string _newWatchlistName = "";
    [ObservableProperty] private string _watchlistNameError = "";
    
    // Inline watchlist rename popup
    [ObservableProperty] private bool _isRenamingWatchlist = false;
    [ObservableProperty] private int? _renamingWatchlistId = null;
    [ObservableProperty] private string _renamingWatchlistName = "";
    [ObservableProperty] private string _renamingWatchlistError = "";

    private readonly Dictionary<string, ScannerRowViewModel> _rowCache = new();
    private readonly ConcurrentQueue<TickData> _batchedTicks = new();
    private readonly System.Timers.Timer _batchTimer;
    private IDisposable? _tickSubscription;
    private bool _disposed = false;

    private const int BatchIntervalMs = 16; // ~60 FPS for smooth updates
    private const int MaxBatchSize = 50;

    public WatchlistViewModel(
        IWatchlistService watchlistService,
        IbkrGatewayService ibkrService,
        IDispatcherService dispatcher,
        ILogger<WatchlistViewModel> logger)
    {
        _watchlistService = watchlistService;
        _ibkrService = ibkrService;
        _dispatcher = dispatcher;
        _logger = logger;

        // Setup batch timer for smooth updates (60 FPS)
        _batchTimer = new System.Timers.Timer(BatchIntervalMs);
        _batchTimer.Elapsed += (_, _) => FlushBatchedTicks();
        _batchTimer.AutoReset = true;
        _batchTimer.Start();

        // Subscribe to tick updates
        _tickSubscription = _ibkrService.TickStream.Subscribe(tick =>
        {
            _batchedTicks.Enqueue(tick);
        });
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            await _watchlistService.InitializeAsync();
            var watchlists = await _watchlistService.GetAllWatchlistsAsync();

            Watchlists.Clear();
            foreach (var w in watchlists)
            {
                Watchlists.Add(w);
            }

            // Select last (most recent) watchlist if available
            if (Watchlists.Count > 0)
            {
                SelectedWatchlist = Watchlists[Watchlists.Count - 1];
            }

            _logger.LogInformation("Initialized watchlist window with {Count} watchlists", Watchlists.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize watchlist window");
            ErrorMessage = $"Failed to load watchlists: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedWatchlistChanged(Watchlist? value)
    {
        if (value != null)
        {
            _ = LoadWatchlistItemsAsync(value.Id);
        }
    }

    private async Task LoadWatchlistItemsAsync(int watchlistId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Clear existing items
            _rowCache.Clear();
            WatchlistItems.Clear();

            // Load items from database
            var items = await _watchlistService.GetWatchlistItemsAsync(watchlistId);
            _logger.LogInformation("Loading {Count} items for watchlist {WatchlistId}", items.Count, watchlistId);

            foreach (var item in items)
            {
                // Create ViewModel for this symbol
                var rowVm = new ScannerRowViewModel(_logger)
                {
                    Symbol = item.Symbol
                };
                _rowCache[item.Symbol] = rowVm;
                WatchlistItems.Add(rowVm);
            }

            _logger.LogInformation("Loaded {Count} items into watchlist view. Market data will arrive via tick stream.", WatchlistItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load watchlist items");
            ErrorMessage = $"Failed to load watchlist: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FlushBatchedTicks()
    {
        if (_batchedTicks.Count == 0 || _disposed)
            return;

        _dispatcher.OnUI(() =>
        {
            var processed = 0;
            while (processed < MaxBatchSize && _batchedTicks.TryDequeue(out var tick))
            {
                if (_rowCache.TryGetValue(tick.Symbol, out var row))
                {
                    tick.ApplyTo(row);  // In-place update using the TickData extension method
                }
                processed++;
            }
        });
    }

    [RelayCommand]
    private async Task CreateWatchlistAsync()
    {
        try
        {
            // Get base name and suggest next available name
            var baseName = "Market Scanner";
            var existingNames = Watchlists.Select(w => w.Name).ToHashSet();
            var number = 1;
            string suggestedName;
            
            do
            {
                suggestedName = $"{baseName} {number}";
                number++;
            } while (existingNames.Contains(suggestedName));

            // Set suggested name and show inline popup
            NewWatchlistName = suggestedName;
            IsCreatingWatchlist = true;
            
            await Task.CompletedTask; // Keep async signature
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show watchlist creation popup");
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConfirmCreateWatchlistAsync()
    {
        try
        {
            // Validate name
            var trimmedName = NewWatchlistName?.Trim() ?? "";
            
            // Validation: Empty or whitespace
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                WatchlistNameError = "Name cannot be empty";
                _logger.LogDebug("Watchlist creation failed - empty name");
                return;
            }
            
            // Validation: Length (max 100 chars per database schema)
            if (trimmedName.Length > 100)
            {
                WatchlistNameError = "Name too long (max 100 characters)";
                _logger.LogDebug("Watchlist creation failed - name too long");
                return;
            }
            
            // Validation: Duplicate name
            if (Watchlists.Any(w => w.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                WatchlistNameError = "Name already exists";
                _logger.LogDebug("Watchlist creation failed - duplicate name");
                return;
            }
            
            // Validation: Invalid characters (prevent special chars that might break UI/DB)
            var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            if (trimmedName.Any(c => invalidChars.Contains(c)))
            {
                WatchlistNameError = "Name contains invalid characters";
                _logger.LogDebug("Watchlist creation failed - invalid characters");
                return;
            }

            // All validations passed - create watchlist
            var newWatchlist = await _watchlistService.CreateWatchlistAsync(trimmedName);
            Watchlists.Add(newWatchlist);
            SelectedWatchlist = newWatchlist;

            _logger.LogInformation("Created empty watchlist '{Name}'", trimmedName);
            
            // Hide popup and clear state
            IsCreatingWatchlist = false;
            NewWatchlistName = "";
            WatchlistNameError = "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create watchlist");
            WatchlistNameError = "Failed to create watchlist";
        }
    }

    [RelayCommand]
    private void CancelCreateWatchlist()
    {
        IsCreatingWatchlist = false;
        NewWatchlistName = "";
        WatchlistNameError = "";
        _logger.LogInformation("Watchlist creation cancelled");
    }

    partial void OnNewWatchlistNameChanged(string value)
    {
        // Clear error when user starts typing
        WatchlistNameError = "";
    }

    [RelayCommand]
    private void SelectWatchlist(Watchlist watchlist)
    {
        SelectedWatchlist = watchlist;
    }

    [RelayCommand]
    private async Task RenameWatchlistAsync(Watchlist watchlist)
    {
        try
        {
            // Set up rename popup with current name pre-filled
            RenamingWatchlistId = watchlist.Id;
            RenamingWatchlistName = watchlist.Name;
            RenamingWatchlistError = "";
            IsRenamingWatchlist = true;
            
            await Task.CompletedTask; // Keep async signature
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show watchlist rename popup");
            RenamingWatchlistError = $"Error: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task ConfirmRenameWatchlistAsync()
    {
        try
        {
            if (!RenamingWatchlistId.HasValue)
            {
                RenamingWatchlistError = "No watchlist selected for rename";
                return;
            }
            
            // Validate name
            var trimmedName = RenamingWatchlistName?.Trim() ?? "";
            
            // Validation: Empty or whitespace
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                RenamingWatchlistError = "Name cannot be empty";
                _logger.LogDebug("Watchlist rename failed - empty name");
                return;
            }
            
            // Validation: Length (max 100 chars per database schema)
            if (trimmedName.Length > 100)
            {
                RenamingWatchlistError = "Name too long (max 100 characters)";
                _logger.LogDebug("Watchlist rename failed - name too long");
                return;
            }
            
            // Validation: Duplicate name (excluding current watchlist)
            if (Watchlists.Any(w => w.Id != RenamingWatchlistId.Value && w.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                RenamingWatchlistError = "Name already exists";
                _logger.LogDebug("Watchlist rename failed - duplicate name");
                return;
            }
            
            // Validation: Invalid characters (prevent special chars that might break UI/DB)
            var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            if (trimmedName.Any(c => invalidChars.Contains(c)))
            {
                RenamingWatchlistError = "Name contains invalid characters";
                _logger.LogDebug("Watchlist rename failed - invalid characters");
                return;
            }

            // All validations passed - rename watchlist
            var success = await _watchlistService.RenameWatchlistAsync(RenamingWatchlistId.Value, trimmedName);
            if (!success)
            {
                RenamingWatchlistError = "Failed to rename watchlist";
                _logger.LogWarning("RenameWatchlistAsync returned false for watchlist {Id}", RenamingWatchlistId.Value);
                return;
            }
            
            // Update watchlist in collection
            var watchlist = Watchlists.FirstOrDefault(w => w.Id == RenamingWatchlistId.Value);
            if (watchlist != null)
            {
                var wasSelected = SelectedWatchlist?.Id == watchlist.Id;
                
                // Update properties
                watchlist.Name = trimmedName;
                watchlist.UpdatedAt = DateTime.UtcNow;
                
                // Trigger collection update by removing and re-adding (ensures UI refresh)
                var index = Watchlists.IndexOf(watchlist);
                Watchlists.RemoveAt(index);
                Watchlists.Insert(index, watchlist);
                
                // Update SelectedWatchlist reference to ensure UI updates
                if (wasSelected)
                {
                    SelectedWatchlist = watchlist;
                }
            }

            _logger.LogInformation("Renamed watchlist {Id} to '{Name}'", RenamingWatchlistId.Value, trimmedName);
            
            // Hide popup and clear state
            IsRenamingWatchlist = false;
            RenamingWatchlistId = null;
            RenamingWatchlistName = "";
            RenamingWatchlistError = "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename watchlist");
            RenamingWatchlistError = "Failed to rename watchlist";
        }
    }
    
    [RelayCommand]
    private void CancelRenameWatchlist()
    {
        IsRenamingWatchlist = false;
        RenamingWatchlistId = null;
        RenamingWatchlistName = "";
        RenamingWatchlistError = "";
        _logger.LogInformation("Watchlist rename cancelled");
    }
    
    partial void OnRenamingWatchlistNameChanged(string value)
    {
        // Clear error when user starts typing
        RenamingWatchlistError = "";
    }

    [RelayCommand]
    private async Task DeleteWatchlistAsync(Watchlist watchlist)
    {
        try
        {
            // Show confirmation dialog
            if (Application.Current?.MainPage == null)
            {
                _logger.LogWarning("Cannot show confirmation dialog - MainPage is null");
                return;
            }

            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Delete Watchlist",
                $"Delete '{watchlist.Name}'?",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                _logger.LogDebug("User cancelled deletion of watchlist '{Name}'", watchlist.Name);
                return;
            }

            await _watchlistService.DeleteWatchlistAsync(watchlist.Id);
            Watchlists.Remove(watchlist);

            if (SelectedWatchlist?.Id == watchlist.Id)
            {
                SelectedWatchlist = Watchlists.FirstOrDefault();
            }

            _logger.LogInformation("Deleted watchlist '{Name}'", watchlist.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete watchlist");
            ErrorMessage = $"Failed to delete watchlist: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RemoveFromWatchlistAsync(ScannerRowViewModel row)
    {
        if (SelectedWatchlist == null)
            return;

        try
        {
            await _watchlistService.RemoveItemAsync(SelectedWatchlist.Id, row.Symbol);
            WatchlistItems.Remove(row);
            _rowCache.Remove(row.Symbol);

            _logger.LogInformation("Removed {Symbol} from watchlist '{Name}'", row.Symbol, SelectedWatchlist.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove symbol from watchlist");
            ErrorMessage = $"Failed to remove symbol: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _disposed = true;

        _batchTimer?.Stop();
        _batchTimer?.Dispose();

        _tickSubscription?.Dispose();

        _rowCache.Clear();
    }
}

