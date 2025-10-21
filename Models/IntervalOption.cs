namespace MarketScanner.Models;

public sealed class IntervalOption
{
    public int Seconds { get; init; }
    public string Text { get; init; } = "";
    public override string ToString() => Text;
}
