using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using MarketScanner.Models;

namespace MarketScanner.Services.Impl;

/// <summary>
/// Simple NDJSON (newline-delimited JSON) tick playback for offline testing.
/// Enable via environment variable PLAYBACK_ENABLED=1. Optional envs:
/// PLAYBACK_FILE (default: ./delayed_ticks.ndjson), PLAYBACK_SPEED (double, default 1.0).
/// Does not touch IBKR code; merely publishes a parallel TickData stream.
/// </summary>
public sealed class DelayedNdjsonTickSource : IDisposable
{
    private readonly Subject<TickData> _subject = new();
    public IObservable<TickData> Stream => _subject.AsObservable();

    private readonly string _filePath;
    private readonly double _speed;
    private CancellationTokenSource? _cts;

    public DelayedNdjsonTickSource(string filePath, double speed = 1.0)
    {
        _filePath = filePath;
        _speed = speed <= 0 ? 1.0 : speed;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                DateTime? firstTs = null;
                DateTime? lastEmitted = null;

                await foreach (var line in ReadLinesAsync(_filePath, token))
                {
                    if (token.IsCancellationRequested) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;

                        var symbol = root.TryGetProperty("symbol", out var sEl) ? sEl.GetString() : null;
                        if (string.IsNullOrWhiteSpace(symbol)) continue;

                        var tsUnix = root.TryGetProperty("ts", out var tsEl) && tsEl.ValueKind == JsonValueKind.Number
                            ? tsEl.GetInt64()
                            : (long?)null;
                        var timestamp = tsUnix.HasValue
                            ? DateTimeOffset.FromUnixTimeMilliseconds(tsUnix.Value).UtcDateTime
                            : DateTime.UtcNow;

                        // Optional fields
                        double? last = TryGetDouble(root, "last");
                        double? close = TryGetDouble(root, "close");
                        long? volume = TryGetLong(root, "volume");
                        double? high = TryGetDouble(root, "high");
                        double? low = TryGetDouble(root, "low");
                        double? open = TryGetDouble(root, "open");
                        long? avgVol = TryGetLong(root, "avgVolume");

                        // Derived change % if available
                        double? change = null;
                        double? changePct = null;
                        if (last.HasValue && close.HasValue && close.Value != 0)
                        {
                            change = last.Value - close.Value;
                            changePct = (change.Value / close.Value) * 100.0;
                        }
                        var changeDec = change.HasValue ? (decimal?)change.Value : null;
                        var changePctDec = changePct.HasValue ? (decimal?)changePct.Value : null;

                        // Optional inter-event delay to preserve timeline at scaled speed
                        if (_speed > 0 && tsUnix.HasValue)
                        {
                            if (firstTs == null) firstTs = timestamp;
                            if (lastEmitted == null) lastEmitted = DateTime.UtcNow;

                            var playedElapsed = (DateTime.UtcNow - lastEmitted.Value).TotalMilliseconds;
                            var origElapsed = (timestamp - firstTs.Value).TotalMilliseconds;
                            var targetElapsed = origElapsed / _speed;
                            var delayMs = targetElapsed - playedElapsed;
                            if (delayMs > 1) await Task.Delay(TimeSpan.FromMilliseconds(delayMs), token);
                        }

                        var tick = new TickData(
                            Symbol: symbol!,
                            SessionId: Guid.Empty,
                            LastPrice: last,
                            ClosePrice: close,
                            Volume: volume,
                            FiftyTwoWeekHigh: null,
                            Timestamp: timestamp,
                            Bid: null,
                            Ask: null,
                            High: high,
                            Low: low,
                            Open: open,
                            PreviousClose: close,
                            AverageVolume: avgVol,
                            RelativeVolume: null,
                            Change: changeDec,
                            ChangePercent: changePctDec
                        );

                        _subject.OnNext(tick);
                        lastEmitted = DateTime.UtcNow;
                    }
                    catch { /* skip malformed lines */ }
                }
            }
            catch { }
        }, token);
    }

    /// <summary>
    /// Attempts to start playback if the file exists. Returns true if playback started.
    /// </summary>
    public bool TryEnsureStarted()
    {
        try
        {
            if (_cts != null) return true;
            if (!File.Exists(_filePath)) return false;
            Start();
            return true;
        }
        catch { return false; }
    }

    private static double? TryGetDouble(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetDouble() : (double?)null;

    private static long? TryGetLong(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt64() : (long?)null;

    private static async IAsyncEnumerable<string> ReadLinesAsync(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        while (!sr.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await sr.ReadLineAsync();
            if (line != null) yield return line;
        }
    }

    public static DelayedNdjsonTickSource? CreateFromEnv()
    {
        var enabled = Environment.GetEnvironmentVariable("PLAYBACK_ENABLED");
        if (string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            var file = Environment.GetEnvironmentVariable("PLAYBACK_FILE");
            if (string.IsNullOrWhiteSpace(file)) file = Path.Combine(AppContext.BaseDirectory, "delayed_ticks.ndjson");
            var speedStr = Environment.GetEnvironmentVariable("PLAYBACK_SPEED");
            double speed = 1.0;
            _ = double.TryParse(speedStr, out speed);
            return new DelayedNdjsonTickSource(file!, speed <= 0 ? 1.0 : speed);
        }
        // Fallback: auto-enable if a delayed_ticks.ndjson file is found nearby (helps with MSIX env var limitations)
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var current = baseDir;
            for (int i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
            {
                var candidate = Path.Combine(current, "delayed_ticks.ndjson");
                if (File.Exists(candidate))
                {
                    return new DelayedNdjsonTickSource(candidate, 1.0);
                }
                current = Directory.GetParent(current)?.FullName ?? string.Empty;
            }
        }
        catch { }
        return null;
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        _subject.OnCompleted();
        _subject.Dispose();
    }
}


