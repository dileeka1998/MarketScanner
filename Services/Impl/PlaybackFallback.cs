using System.Reactive.Linq;
using System.Reactive.Subjects;
using MarketScanner.Models;

namespace MarketScanner.Services.Impl;

/// <summary>
/// Coordinates fallback to NDJSON playback or synthetic ticks when IBKR is unavailable.
/// Maintains latest snapshots per symbol for seeding UI after refresh.
/// </summary>
public sealed class PlaybackFallback : IDisposable
{
    private readonly Subject<TickData> _stream = new();
    public IObservable<TickData> TickStream => _stream.AsObservable();

    private DelayedNdjsonTickSource? _ndjson;
    private SyntheticTickSource? _synthetic;
    private IDisposable? _ndjsonSub;
    private IDisposable? _syntheticSub;
    private bool _active;

    private readonly Dictionary<string, TickData> _latestBySymbol = new(StringComparer.OrdinalIgnoreCase);
    private string[] _currentSymbols = Array.Empty<string>();
    private string[] _universe = Array.Empty<string>();

    public bool IsActive => _active;
    public IReadOnlyList<string> CurrentSymbols => _currentSymbols;

    public bool ActivateIfNeeded(IEnumerable<string>? preferredSymbols = null)
    {
        if (_active) return true;

        // Try NDJSON first (auto-discovery already supported internally)
        _ndjson = DelayedNdjsonTickSource.CreateFromEnv();
        if (_ndjson != null)
        {
            _ndjson.Start();
            _ndjsonSub = _ndjson.Stream.Subscribe(OnTick);
            _active = true;
            if (preferredSymbols != null) _currentSymbols = preferredSymbols.Distinct().ToArray();
            return true;
        }

        // Fall back to synthetic using full universe (200 symbols) - always generate ticks for all symbols
        _universe = DefaultSymbols().ToArray();
        // Always use full universe for tick generation to ensure SelectSymbols can filter across all symbols
        _synthetic = new SyntheticTickSource(_universe, TimeSpan.FromMilliseconds(150));
        _synthetic.Start();
        _syntheticSub = _synthetic.Stream.Subscribe(OnTick);
        // Store preferred symbols only for tracking, but don't limit tick generation
        _currentSymbols = (preferredSymbols != null && preferredSymbols.Any()) 
            ? preferredSymbols.Distinct().ToArray() 
            : _universe.ToArray();
        _active = true;
        return true;
    }

    public void UpdateSymbols(IEnumerable<string> symbols)
    {
        var set = symbols?.Distinct().ToArray() ?? Array.Empty<string>();
        _currentSymbols = set;
        // DON'T recreate SyntheticTickSource - keep it generating ticks for full universe
        // This ensures _latestBySymbol always has data for all symbols in universe
        // SelectSymbols can then filter by price across the entire universe
        // NDJSON emits whatever is in file; we just change seeding list
    }

    /// <summary>
    /// Returns a symbol list respecting TopN and price filters using latest tick data.
    /// Returns more symbols than TopN to give FilterEngine a pool to filter from (volume, change%, etc.).
    /// </summary>
    public IReadOnlyList<string> SelectSymbols(int topN, decimal? minPrice, decimal? maxPrice)
    {
        if (_universe == null || _universe.Length == 0)
            _universe = DefaultSymbols().ToArray();
        if (topN <= 0) topN = 5;

        // Return a larger pool (3x TopN) so FilterEngine can filter by volume, change%, etc.
        // and still have enough symbols to satisfy TopN after filtering
        // Only apply minimum for very small TopN values
        var poolSize = topN >= 5 ? topN * 3 : Math.Max(topN * 3, 15);

        // If no price filters, return a broader pool
        if (minPrice == null && maxPrice == null)
            return _universe.Take(poolSize).ToArray();

        // Filter by price using latest tick data, then return a broader pool
        var candidates = _universe.Where(s =>
        {
            if (!_latestBySymbol.TryGetValue(s, out var tick)) return false; // Skip if no tick data yet
            var price = (decimal)tick.LastPrice;
            if (minPrice.HasValue && price < minPrice.Value) return false;
            if (maxPrice.HasValue && price > maxPrice.Value) return false;
            return true;
        }).Take(poolSize).ToArray();

        // If we have some candidates, return them
        if (candidates.Length > 0) return candidates;

        // Fallback: if no tick data matches, return a broader set (let FilterEngine filter client-side)
        // This handles initial state before ticks arrive
        return _universe.Take(poolSize).ToArray();
    }

    public IReadOnlyDictionary<string, TickData> GetLatestSnapshots(IEnumerable<string> forSymbols)
    {
        var dict = new Dictionary<string, TickData>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in forSymbols)
        {
            if (_latestBySymbol.TryGetValue(s, out var t))
                dict[s] = t;
        }
        return dict;
    }

    private void OnTick(TickData t)
    {
        _latestBySymbol[t.Symbol] = t;
        _stream.OnNext(t);
    }

    private static IEnumerable<string> DefaultSymbols()
    {
        // Seed list + generated to reach ~200 symbols
        var seeds = new[] { "AAPL","MSFT","NVDA","AMD","TSLA","META","AMZN","GOOGL","SPY","QQQ","NFLX","UBER","SHOP","INTC","BABA","BAC","JPM","KO","PEP","DIS","SIRI","F","NIO","SNAP","PLUG","SOFI","RIOT","CHPT","CCL","NOK" };
        var list = new List<string>(seeds);
        for (int i = 1; i <= 200; i++) list.Add($"SYM{i:000}");
        return list.Distinct(StringComparer.OrdinalIgnoreCase).Take(200);
    }

    public void Dispose()
    {
        try { _ndjsonSub?.Dispose(); } catch { }
        try { _syntheticSub?.Dispose(); } catch { }
        try { _ndjson?.Dispose(); } catch { }
        try { _synthetic?.Dispose(); } catch { }
        _stream.OnCompleted();
        _stream.Dispose();
    }
}


