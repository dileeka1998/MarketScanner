using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketScanner.Models;
using MarketScanner.Services;
using MarketScanner.Services.Ibkr;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;

namespace MarketScanner.ViewModels;

public partial class QuoteViewModel : ObservableObject, IDisposable
{
    private readonly IbkrGatewayService _ibkrService;
    private readonly IDispatcherService _dispatcher;
    private readonly ILogger<QuoteViewModel> _logger;

    [ObservableProperty] private ObservableCollection<ScannerRowViewModel> _quoteItems = new();
    [ObservableProperty] private string _newSymbolText = "";
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    private readonly Dictionary<string, ScannerRowViewModel> _rowCache = new();
    private readonly ConcurrentQueue<TickData> _batchedTicks = new();
    private readonly System.Timers.Timer _batchTimer;
    private IDisposable? _tickSubscription;
    private IDisposable? _playbackSubscription;
    private MarketScanner.Services.Impl.DelayedNdjsonTickSource? _playbackSource;
    private bool _disposed = false;

    private const int BatchIntervalMs = 16; // ~60 FPS for smooth updates
    private const int MaxBatchSize = 50;

    public QuoteViewModel(
        IbkrGatewayService ibkrService,
        IDispatcherService dispatcher,
        ILogger<QuoteViewModel> logger)
    {
        _ibkrService = ibkrService;
        _dispatcher = dispatcher;
        _logger = logger;

        // Setup batch timer for smooth updates (60 FPS)
        _batchTimer = new System.Timers.Timer(BatchIntervalMs);
        _batchTimer.Elapsed += (_, _) => FlushBatchedTicks();
        _batchTimer.AutoReset = true;
        _batchTimer.Start();

        // Subscribe to tick updates (IBKR)
        _tickSubscription = _ibkrService.TickStream.Subscribe(tick =>
        {
            _batchedTicks.Enqueue(tick);
        });

        // Fallback playback stream (NDJSON or synthetic via controller)
        var fb = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services?.GetService<MarketScanner.Services.Impl.PlaybackFallback>();
        if (fb != null && fb.IsActive)
        {
            _playbackSubscription = fb.TickStream.Subscribe(t => _batchedTicks.Enqueue(t));
        }
        else
        {
            // Legacy NDJSON env-based attach (best-effort)
            _playbackSource = MarketScanner.Services.Impl.DelayedNdjsonTickSource.CreateFromEnv();
            if (_playbackSource != null)
            {
                _playbackSource.Start();
                _playbackSubscription = _playbackSource.Stream.Subscribe(tick => _batchedTicks.Enqueue(tick));
            }
        }
    }

    public Task InitializeAsync()
    {
        _logger.LogInformation("Quote panel initialized");
        return Task.CompletedTask;
    }

    public async Task AddQuotesFromScannerAsync(IEnumerable<ScannerRowViewModel> rows)
    {
        try
        {
            if (rows == null) return;

            // Build list to add (skip duplicates)
            foreach (var r in rows)
            {
                var symbol = r.Symbol?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(symbol)) continue;
                if (_rowCache.ContainsKey(symbol)) continue;

                var rowVm = new ScannerRowViewModel(_logger)
                {
                    Symbol = symbol,
                    Company = r.Company,
                    Region = r.Region,
                    Product = r.Product,
                    Exchange = r.Exchange
                };

                // Seed with current values so UI shows something immediately; live ticks will update
                rowVm.LastPrice = r.LastPrice;
                rowVm.Volume = r.Volume;
                rowVm.AvgVolume = r.AvgVolume;
                // Seed previous close if available to enable Change/Change% immediately
                if (r.PrevClose > 0)
                    rowVm.UpdateClosePrice(r.PrevClose);

                _rowCache[symbol] = rowVm;
                QuoteItems.Add(rowVm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add quotes from scanner");
            ErrorMessage = $"Failed to add quotes: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    public async Task SyncToSymbols(IEnumerable<string> symbols)
    {
        try
        {
            var target = new HashSet<string>(symbols.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);

            // Try to get latest snapshots from fallback if available
            var fb = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services?.GetService<MarketScanner.Services.Impl.PlaybackFallback>();
            var latest = fb != null ? fb.GetLatestSnapshots(target) : new Dictionary<string, TickData>();

            // Remove missing
            var toRemove = _rowCache.Keys.Where(k => !target.Contains(k)).ToList();
            foreach (var k in toRemove)
            {
                if (_rowCache.TryGetValue(k, out var vm))
                {
                    QuoteItems.Remove(vm);
                    _rowCache.Remove(k);
                }
            }

            // Add new or update existing
            foreach (var s in target)
            {
                if (_rowCache.TryGetValue(s, out var existingVm))
                {
                    // Update existing item with latest tick data if available
                    if (latest.TryGetValue(s, out var tick))
                    {
                        tick.ApplyTo(existingVm);
                        if (tick.PreviousClose.HasValue && tick.PreviousClose.Value > 0)
                            existingVm.UpdateClosePrice((double)tick.PreviousClose.Value);
                    }
                }
                else
                {
                    // Add new
                    var vm = new ScannerRowViewModel(_logger)
                    {
                        Symbol = s,
                        Company = s,
                        Region = "United States",
                        Product = "Stocks",
                        Exchange = "us stocks"
                    };
                    // Seed with latest tick data if available
                    if (latest.TryGetValue(s, out var tick))
                    {
                        tick.ApplyTo(vm);
                        if (tick.PreviousClose.HasValue && tick.PreviousClose.Value > 0)
                            vm.UpdateClosePrice((double)tick.PreviousClose.Value);
                    }
                    _rowCache[s] = vm;
                    QuoteItems.Add(vm);
                }
            }

            _logger.LogInformation("Synced quotes to {Count} symbols", target.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync quotes to symbols");
            ErrorMessage = $"Failed to sync quotes: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    private void FlushBatchedTicks()
    {
        if (_batchedTicks.Count == 0 || _disposed)
            return;

        try
        {
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("Unable to find main thread"))
        {
            // UI not ready yet or app shutting down - just skip this batch
            // This can happen during app initialization or shutdown when the main thread is unavailable
        }
    }

    [RelayCommand]
    private async Task AddQuoteAsync()
    {
        try
        {
            var symbol = NewSymbolText?.Trim().ToUpperInvariant() ?? "";

            if (string.IsNullOrWhiteSpace(symbol))
            {
                ErrorMessage = "Please enter a symbol";
                return;
            }

            // Check if already added
            if (_rowCache.ContainsKey(symbol))
            {
                ErrorMessage = $"Symbol {symbol} is already in quotes";
                return;
            }

            ErrorMessage = "";

            // Create ViewModel for this symbol
            var rowVm = new ScannerRowViewModel(_logger)
            {
                Symbol = symbol
            };

            _rowCache[symbol] = rowVm;
            QuoteItems.Add(rowVm);

            NewSymbolText = "";
            _logger.LogInformation("Added symbol {Symbol} to quotes", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add quote");
            ErrorMessage = $"Failed to add quote: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RemoveQuote(ScannerRowViewModel row)
    {
        try
        {
            var symbol = row.Symbol;
            QuoteItems.Remove(row);
            _rowCache.Remove(symbol);
            _logger.LogInformation("Removed symbol {Symbol} from quotes", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove quote");
            ErrorMessage = $"Failed to remove quote: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearQuotes()
    {
        try
        {
            QuoteItems.Clear();
            _rowCache.Clear();
            ErrorMessage = "";
            _logger.LogInformation("Cleared all quotes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear quotes");
            ErrorMessage = $"Failed to clear quotes: {ex.Message}";
        }
    }

    partial void OnNewSymbolTextChanged(string value)
    {
        // Clear error when user starts typing
        ErrorMessage = "";
    }

    public void Dispose()
    {
        _disposed = true;
        
        try
        {
            _batchTimer?.Stop();
            _batchTimer?.Dispose();
        }
        catch { }

        try
        {
            _tickSubscription?.Dispose();
        }
        catch { }

        try
        {
            _playbackSubscription?.Dispose();
        }
        catch { }

        try
        {
            _playbackSource?.Dispose();
        }
        catch { }

        QuoteItems.Clear();
        _rowCache.Clear();
    }
}
