namespace MarketScanner.Models;

public sealed class TickEventArgs : EventArgs
{
    public string Symbol { get; init; } = string.Empty;
    public int Field { get; init; }
    public object? Value { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public delegate void TickReceivedEventHandler(object sender, TickEventArgs e);
