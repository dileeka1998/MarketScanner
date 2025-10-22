using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketScanner.Core;
using MarketScanner.Models;
using MarketScanner.Services;
using MarketScanner.Services.Ibkr;
using MarketScanner.Utilities;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;

namespace MarketScanner.ViewModels;

public partial class ScannerViewModel : ObservableObject
{
    private readonly IScanner _scanner;
    private readonly IDispatcherService _dispatcher;
    private readonly ILogger<ScannerViewModel> _logger;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _filterCts;
    private int _applyEpoch; // NEW: prevents out-of-order commits
    private bool _disposed = false;

    // Track previous scan-level filters for hybrid filtering
    private (decimal minPrice, decimal maxPrice, string region, string product, string exchange, int topN, decimal? minChgPct)? _previousScanFilters;

    // UI batching for ultra-smooth updates (60 FPS)
    private readonly ConcurrentQueue<TickData> _batchedTicks = new();
    private readonly System.Timers.Timer _batchTimer;
    private readonly Dictionary<string, ScannerRowViewModel> _rowLookup = new();
    private bool _uiReady = false;

    private const int BatchIntervalMs = 16; // ~60 FPS for smooth updates
    private const int MaxBatchSize = 50;

    [ObservableProperty] private ObservableCollection<ScannerRowViewModel> _scannerItems = new();
    [ObservableProperty] private string _debugStatus = "";

    // Filter properties

    [ObservableProperty] private string _exchange = "US Stocks";
    [ObservableProperty] private string _minPriceText = "";
    [ObservableProperty] private string _maxPriceText = "";
    [ObservableProperty] private string _minChangePercentText = "";
    [ObservableProperty] private string _volumeMinText = "";
    [ObservableProperty] private int _topN = 25;
    [ObservableProperty] private bool _isRefreshing = false;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _autoRefreshEnabled = false;
    [ObservableProperty] private int _refreshIntervalSeconds = 60; // default 60

    // Options for pickers

    public List<string> ExchangeOptions { get; } = new() { "us stocks", "nasdaq", "nyse", "amex", "otc" };
    public List<int> TopNOptions { get; } = new() { 5, 10, 15, 20, 50 };

    private readonly Debounce _debounce = new(TimeSpan.FromMilliseconds(50)); // very responsive for production use

    // Auto-refresh timer fields
    private CancellationTokenSource? _autoCts;
    private Task? _autoTask;

    // Property change handlers - all use debounced filtering
    partial void OnMinChangePercentTextChanged(string value) => DebouncedApply();
    partial void OnVolumeMinTextChanged(string value) => DebouncedApply();

    // Auto-refresh property change handlers
    partial void OnRefreshIntervalSecondsChanged(int oldValue, int newValue)
    {
        // Persist selection
        Preferences.Set("refresh.interval.seconds", newValue);
        // If auto-refresh is on, restart quickly
        RestartAutoRefreshTimerIfNeeded();
    }

    partial void OnAutoRefreshEnabledChanged(bool oldValue, bool newValue)
    {
        Preferences.Set("refresh.enabled", newValue);
        if (newValue)
            RestartAutoRefreshTimerIfNeeded();
        else
        {
            // Stop auto-refresh immediately (synchronous cancellation)
            try { _autoCts?.Cancel(); } catch { }
            _autoTask = null;  // Don't wait for task, just null it
        }
    }

    public ScannerViewModel(IScanner scanner, IDispatcherService dispatcher, ILogger<ScannerViewModel> logger)
    {
        _scanner = scanner;
        _dispatcher = dispatcher;
        _logger = logger;

        // Setup batch timer for ultra-smooth updates (60 FPS) FIRST
        _batchTimer = new System.Timers.Timer(BatchIntervalMs);
        _batchTimer.Elapsed += (_, _) => FlushBatchedTicks();
        _batchTimer.AutoReset = true;
        // Don't start timer yet - wait for UI to be ready

        // Set production defaults AFTER timer is initialized
        SetProductionDefaults();

        // Subscribe to tick stream and queue for batching
        if (scanner is IbkrGatewayService ibkrGateway)
        {
            ibkrGateway.TickStream.Subscribe(tick =>
            {
                _batchedTicks.Enqueue(tick);
            });
        }

        // Subscribe to property changes for debounced filtering
        PropertyChanged += (_, e) =>
        {
            // Auto-reset exchange to "any" when region changes to non-US to avoid IBKR mismatch errors

            {

                {

                }
            }

            if (e.PropertyName?.StartsWith("Min") == true ||
                e.PropertyName?.StartsWith("Max") == true ||
                e.PropertyName?.StartsWith("Selected") == true ||
                e.PropertyName?.StartsWith("TopN") == true ||
                e.PropertyName?.StartsWith("Exchange") == true)
            {
                DebouncedApply();
            }
        };

        // Load refresh preferences after initialization
        LoadRefreshPrefs();

        // Wire auto-refresh property changes
        WireAutoRefresh();
    }

    private void SetProductionDefaults()
    {
        // Set production defaults for IBKR scanner

        Exchange = "us stocks";
        MinPriceText = "2";
        MaxPriceText = "20";
        VolumeMinText = "100000";
        TopN = 50;
        MinChangePercentText = "";  // No default change% filter

        _logger.LogInformation("Set production defaults: Price={MinPrice}-{MaxPrice}, Volume={VolumeMin}, TopN={TopN}",
            MinPriceText, MaxPriceText, VolumeMinText, TopN);
    }

    public void ResetToDefaults()
    {

        Exchange = "us stocks";
        MinPriceText = "2";
        MaxPriceText = "20";
        VolumeMinText = "100000";
        TopN = 50;

        _logger.LogInformation("Filters reset to production defaults");
    }

    private void DebouncedApply() => _ = _debounce.ExecuteAsync(ApplyFiltersAsync);

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsRefreshing || IsLoading) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            IsRefreshing = true;
            ErrorMessage = "";
            DebugStatus = "Loading data...";

            // Start batch timer now that we're refreshing (UI should be ready)
            StartBatchTimer();

            _logger.LogInformation("Starting data refresh...");

            // Get current scan-level filter values
            var minPrice = ParseDecimalSafe(MinPriceText) ?? 2;
            var maxPrice = ParseDecimalSafe(MaxPriceText) ?? 20;
            const string product = "stocks";  // Always stocks
            var exchange = IsAnyValue(Exchange) ? "us stocks" : Exchange.ToLowerInvariant();

            // Get fresh data using the scanner service with dynamic parameters
            // Cast to concrete type to access overloaded ScanAsync method
            var rows = _scanner is IbkrGatewayService ibkrGateway
                ? await ibkrGateway.ScanAsync(minPrice, maxPrice, product, exchange, TopN, _cts.Token)
                : await _scanner.ScanAsync(_cts.Token);

            _logger.LogInformation("Received {Count} rows from scanner", rows.Count);

            // Clear existing rows and rebuild from scanner results
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ScannerItems.Clear();
                _rowLookup.Clear();

                foreach (var row in rows)
                {
                    var rowVm = new ScannerRowViewModel
                    {
                        Symbol = row.Symbol,
                        Company = row.Company ?? row.Symbol,
                        Region = "United States",  // Always US
                        Product = row.Meta.Product ?? "Stocks",
                        Exchange = row.Meta.Exchange ?? Exchange,
                        LastPrice = (double)row.LastPrice,
                        PrevClose = (double)row.LastPrice, // Will be updated by market data
                        Volume = (long)row.Volume,
                        AvgVolume = (long)row.AvgVolume
                    };
                    // RelativeVolume is auto-calculated in ScannerRowViewModel

                    _rowLookup[row.Symbol] = rowVm;
                    ScannerItems.Add(rowVm);
                }

                _logger.LogInformation("Created {Count} ScannerRowViewModel instances", ScannerItems.Count);
            });

            // Re-apply client-side filters (TopN, MinChangePercent, Volume)
            await ApplyFiltersAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during refresh");
            ErrorMessage = ex.Message;
            DebugStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
            DebugStatus = $"ScannerItems: {ScannerItems.Count}";
            _logger.LogInformation("RefreshAsync completed: ScannerItems={ScannerItemsCount}", ScannerItems.Count);
        }
    }

    private void FlushBatchedTicks()
    {
        if (_batchedTicks.Count == 0 || !_uiReady) return;

        try
        {
            _dispatcher.OnUI(() =>
            {
                var processedCount = 0;
                var updatedSymbols = new HashSet<string>();

                while (_batchedTicks.TryDequeue(out var tick) && processedCount < MaxBatchSize)
                {
                    var rowVm = GetOrCreateRow(tick.Symbol);
                    tick.ApplyTo(rowVm);  // In-place update!
                    // RelativeVolume is auto-calculated in ScannerRowViewModel
                    updatedSymbols.Add(tick.Symbol);
                    processedCount++;
                }

                if (processedCount > 0)
                {
                    DebugStatus = $"Updated {updatedSymbols.Count} symbols ({processedCount} ticks)";
                }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Unable to find main thread"))
        {
            // UI not ready yet - just skip this batch
            _logger.LogDebug("Skipping tick batch - main thread not available yet");
        }
    }

    private void StartBatchTimer()
    {
        if (_batchTimer == null)
        {
            _logger.LogError("Batch timer is null - cannot start");
            return;
        }

        if (!_batchTimer.Enabled)
        {
            _uiReady = true;
            _batchTimer.Start();
            _logger.LogInformation("Batch timer started - ready for tick updates");
        }
    }

    private ScannerRowViewModel GetOrCreateRow(string symbol)
    {
        if (!_rowLookup.TryGetValue(symbol, out var rowVm))
        {
            rowVm = new ScannerRowViewModel
            {
                Symbol = symbol,
                Company = symbol,
                Region = "United States",  // Always US
                Product = "Stocks",
                Exchange = Exchange
            };
            _rowLookup[symbol] = rowVm;
            ScannerItems.Add(rowVm);
        }
        return rowVm;
    }

    private static bool IsAnyValue(string? s) =>
        string.IsNullOrWhiteSpace(s)
        || s.Equals("any", StringComparison.OrdinalIgnoreCase)
        || s.Equals("all", StringComparison.OrdinalIgnoreCase)
        || s.Equals("us", StringComparison.OrdinalIgnoreCase)
        || s.Equals("stocks", StringComparison.OrdinalIgnoreCase)
        || s.Equals("us stocks", StringComparison.OrdinalIgnoreCase)
        || s.Equals("-", StringComparison.OrdinalIgnoreCase);

    private static decimal? ParseDecimalSafe(string? s) => Parsing.ParseDecimal(s);
    private static decimal? ParsePercentSafe(string? s) => Parsing.ParsePercent(s);

    /// <summary>
    /// Determines if the current filter changes require a re-scan at IBKR level.
    /// Re-scan triggers: price range, exchange, TopN, MinChgPct changes.
    /// Client-side only: volume.
    /// Note: All scan-level changes trigger re-scan with fresh IBKR data.
    /// </summary>
    private bool RequiresRescan()
    {
        var currentMinPrice = ParseDecimalSafe(MinPriceText) ?? 2;
        var currentMaxPrice = ParseDecimalSafe(MaxPriceText) ?? 20;
        const string currentRegion = "us";  // Always US
        const string currentProduct = "stocks";  // Always stocks
        var currentExchange = IsAnyValue(Exchange) ? "us stocks" : Exchange.ToLowerInvariant();
        var currentTopN = TopN;
        var currentMinChgPct = ParsePercentSafe(MinChangePercentText);

        var currentScanFilters = (currentMinPrice, currentMaxPrice, currentRegion, currentProduct, currentExchange, currentTopN, currentMinChgPct);

        // If no previous scan, we need to scan
        if (_previousScanFilters == null)
        {
            _previousScanFilters = currentScanFilters;
            return true;
        }

        // Check if scan-level filters changed
        var requiresRescan = _previousScanFilters.Value != currentScanFilters;

        if (requiresRescan)
        {
            _logger.LogInformation("Scan-level filters changed: {Previous} -> {Current}, triggering re-scan",
                _previousScanFilters.Value, currentScanFilters);
            _previousScanFilters = currentScanFilters;
        }

        return requiresRescan;
    }

    private async Task ApplyFiltersAsync()
    {
        if (_disposed) return;

        // Check if we need to re-scan at IBKR level
        if (RequiresRescan())
        {
            _logger.LogInformation("Significant filter changes detected, triggering re-scan");
            await RefreshAsync();
            return;
        }

        // If no items, skip client-side filtering
        if (ScannerItems.Count == 0)
        {
            _logger.LogInformation("ApplyFiltersAsync skipped - no items to filter");
            return;
        }

        _logger.LogInformation("ApplyFiltersAsync started with {Count} items (client-side filtering only)", ScannerItems.Count);

        var epoch = Interlocked.Increment(ref _applyEpoch);

        // Build criteria (treat default labels as no-op)
        // Note: TopN is now handled at IBKR level, not client-side
        var criteria = new FilterEngine.Criteria(

            Exchange: IsAnyValue(Exchange) ? null : Exchange,
            MinPrice: ParseDecimalSafe(MinPriceText),
            MaxPrice: ParseDecimalSafe(MaxPriceText),
            MinChgPct: ParsePercentSafe(MinChangePercentText),
            MinVolume: ParseDecimalSafe(VolumeMinText) != null ? (long)ParseDecimalSafe(VolumeMinText)!.Value : null,
            TopN: TopN // Pass the actual TopN value from ViewModel
        );

        _logger.LogInformation("Filter criteria: MinPrice={MinPrice}, MaxPrice={MaxPrice}, MinVolume={MinVolume}, MinChgPct={MinChgPct}, TopN={TopN}",
            criteria.MinPrice, criteria.MaxPrice, criteria.MinVolume, criteria.MinChgPct, criteria.TopN);

        var rows = ScannerItems.ToArray(); // copy to array for fast indexer

        // Cancel previous filter operation and create new one
        try
        {
            _filterCts?.Cancel();
            _filterCts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, ignore
        }
        _filterCts = new CancellationTokenSource();
        var token = _filterCts?.Token ?? CancellationToken.None;

        // Check if cancelled before expensive operation
        if (_filterCts?.IsCancellationRequested == true) return;

        var result = await Task.Run(() => FilterEngine.Apply(rows, criteria), token);

        if (epoch != _applyEpoch)
        {
            _logger.LogInformation("ApplyFiltersAsync cancelled - stale compute");
            return; // stale compute – ignore
        }

        _logger.LogInformation("FilterEngine.Apply returned {FilteredCount} of {TotalCount} items", result.TopIndices.Length, rows.Length);

        // Marshal to UI thread once with the FILTERED set
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ScannerItems.Clear();
            foreach (var i in result.TopIndices)
                ScannerItems.Add(rows[i]);

            _logger.LogInformation("ScannerItems updated on UI thread with {Count} items", ScannerItems.Count);

            DebugStatus =
                $"Items: {rows.Length} | Shown: {ScannerItems.Count} | TopN={criteria.TopN} " +
                $"| Price=[{criteria.MinPrice?.ToString() ?? "-"}, {criteria.MaxPrice?.ToString() ?? "-"}] " +
                $"| Vol>={(criteria.MinVolume?.ToString() ?? "-")}";
        });
    }


    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            await RefreshAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load refresh preferences from storage
    /// </summary>
    public void LoadRefreshPrefs()
    {
        RefreshIntervalSeconds = Preferences.Get("refresh.interval.seconds", 60);
        AutoRefreshEnabled = Preferences.Get("refresh.enabled", false);
    }

    /// <summary>
    /// Wire auto-refresh property changes to trigger refresh logic
    /// </summary>
    private void WireAutoRefresh()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AutoRefreshEnabled) || e.PropertyName == nameof(RefreshIntervalSeconds))
            {
                RestartAutoRefreshTimerIfNeeded();
            }
        };
    }

    /// <summary>
    /// Stop the auto-refresh timer
    /// </summary>
    public async Task StopAutoRefreshAsync()
    {
        try
        {
            _autoCts?.Cancel();
            _autoCts?.Dispose();
            _autoCts = null;
        }
        catch { }

        if (_autoTask != null)
        {
            try { await _autoTask; } catch { }
            _autoTask = null;
        }
    }

    /// <summary>
    /// Restart the auto-refresh timer if enabled
    /// </summary>
    public void RestartAutoRefreshTimerIfNeeded()
    {
        _ = Task.Run(async () =>
        {
            await StopAutoRefreshAsync();
            if (!AutoRefreshEnabled) return;

            var cts = new CancellationTokenSource();
            var token = cts.Token;  // Capture token BEFORE assigning to field
            _autoCts = cts;  // Now assign to field

            // Use captured token (safe even if cts gets disposed)
            _autoTask = Task.Run(() => RunAutoRefreshLoopAsync(token));
        });
    }

    /// <summary>
    /// Run the auto-refresh loop with proper error handling
    /// </summary>
    private async Task RunAutoRefreshLoopAsync(CancellationToken ct)
    {
        // Start with immediate refresh
        await RefreshAsync().ConfigureAwait(false);

        while (!ct.IsCancellationRequested)
        {
            var delay = Math.Max(5, RefreshIntervalSeconds); // floor to 5s
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), ct).ConfigureAwait(false);
                if (!ct.IsCancellationRequested)
                    await RefreshAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException) { }
        }
    }

    /// <summary>
    /// Handle auto-refresh toggle
    /// </summary>
    public async Task OnAutoRefreshToggledAsync(bool isEnabled)
    {
        if (isEnabled)
            RestartAutoRefreshTimerIfNeeded();
        else
            await StopAutoRefreshAsync();
    }

    private IRelayCommand? _resetFiltersCommand;
    public IRelayCommand ResetFiltersCommand => _resetFiltersCommand ??=
        new RelayCommand(async () =>
        {

            Exchange = "us stocks";
            MinPriceText = "2";
            MaxPriceText = "20";
            VolumeMinText = "100000";
            MinChangePercentText = "";
            TopN = 50;

            await ApplyFiltersAsync();

            // Stop auto-refresh timer when resetting filters
            await StopAutoRefreshAsync();
        });

    public void Dispose()
    {
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        // Ensure proper disposal order for filter CTS
        _filterCts?.Cancel();
        Task.Delay(50).Wait();  // Give pending operations time to cancel
        _filterCts?.Dispose();

        // Stop batch timer
        _batchTimer?.Stop();
        _batchTimer?.Dispose();

        _ = StopAutoRefreshAsync();
        _autoCts?.Dispose();
        _debounce.Dispose();

        // Stop scanner
        _ = Task.Run(async () => await _scanner.StopAsync());
    }
}
