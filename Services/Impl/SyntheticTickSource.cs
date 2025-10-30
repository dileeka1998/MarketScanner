using System.Reactive.Linq;
using System.Reactive.Subjects;
using MarketScanner.Models;

namespace MarketScanner.Services.Impl;

/// <summary>
/// Synthetic random-walk tick generator for offline testing.
/// Emits TickData for a configured set of symbols at a fixed cadence.
/// </summary>
public sealed class SyntheticTickSource : IDisposable
{
    private readonly Subject<TickData> _subject = new();
    public IObservable<TickData> Stream => _subject.AsObservable();

    private readonly string[] _symbols;
    private readonly TimeSpan _interval;
    private readonly Random _random = new();
    private CancellationTokenSource? _cts;

    private readonly Dictionary<string, (double last, double close, long vol, long avgVol)> _state = new();

    public SyntheticTickSource(IEnumerable<string> symbols, TimeSpan? interval = null)
    {
        _symbols = symbols?.Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()).ToArray() ?? Array.Empty<string>();
        _interval = interval ?? TimeSpan.FromMilliseconds(150);
        foreach (var s in _symbols)
        {
            // Constrain to filter-friendly ranges
            var basePx = 5.0 + _random.NextDouble() * 13.0; // 5..18
            var close = basePx * (0.98 + _random.NextDouble() * 0.04); // +/-2%
            var avgVol = (long)(5_000_000 + _random.NextDouble() * 55_000_000); // 5M..60M
            var startVol = 120_000 + _random.Next(0, 50_000); // >= 120k
            _state[s] = (basePx, close, startVol, avgVol);
        }
    }

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    foreach (var s in _symbols)
                    {
                        var st = _state[s];
                        // Random walk
                        var drift = ( _random.NextDouble() - 0.5 ) * 0.4; // +/-0.2%
                        var newLast = Math.Max(0.5, st.last * (1.0 + drift / 100.0));
                        var volDelta = (long)(100 + _random.Next(0, 5000));
                        var newVol = st.vol + volDelta;
                        _state[s] = (newLast, st.close, newVol, st.avgVol);

                        var change = (decimal?)(newLast - st.close);
                        var changePct = st.close != 0 ? (decimal?)((newLast - st.close) / st.close * 100.0) : null;

                        _subject.OnNext(new TickData(
                            Symbol: s,
                            SessionId: Guid.Empty,
                            LastPrice: newLast,
                            ClosePrice: st.close,
                            Volume: newVol,
                            FiftyTwoWeekHigh: null,
                            Timestamp: now,
                            Bid: null,
                            Ask: null,
                            High: null,
                            Low: null,
                            Open: null,
                            PreviousClose: st.close,
                            AverageVolume: st.avgVol,
                            RelativeVolume: null,
                            Change: change,
                            ChangePercent: changePct
                        ));
                    }

                    await Task.Delay(_interval, token);
                }
            }
            catch { }
        }, token);
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        _subject.OnCompleted();
        _subject.Dispose();
    }
}


