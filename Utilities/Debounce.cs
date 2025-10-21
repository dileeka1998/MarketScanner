namespace MarketScanner.Utilities;

public sealed class Debouncer
{
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    
    public void Debounce(TimeSpan delay, Func<Task> action)
    {
        CancellationTokenSource? current;
        lock (_gate)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            current = _cts;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, current!.Token);
                if (!current.IsCancellationRequested) await action();
            }
            catch (TaskCanceledException) { }
        });
    }
}

public sealed class Debounce : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;

    public Debounce(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        CancellationTokenSource? current;
        lock (_gate)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            current = _cts;
        }
        await Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delay, current!.Token);
                if (!current.IsCancellationRequested) await action();
            }
            catch (TaskCanceledException) { }
        });
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}