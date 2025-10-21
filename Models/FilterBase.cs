namespace MarketScanner.Models;

public abstract class FilterBase
{
    public abstract bool IsActive { get; }
    public abstract IEnumerable<ScannerItem> Apply(IEnumerable<ScannerItem> items);
}

public class RangeFilter<T> : FilterBase where T : struct, IComparable<T>
{
    private readonly Func<ScannerItem, T> _selector;
    private readonly T? _minValue;
    private readonly T? _maxValue;

    public RangeFilter(Func<ScannerItem, T> selector, T? minValue, T? maxValue)
    {
        _selector = selector;
        _minValue = minValue;
        _maxValue = maxValue;
    }

    public override bool IsActive => _minValue.HasValue || _maxValue.HasValue;

    public override IEnumerable<ScannerItem> Apply(IEnumerable<ScannerItem> items)
    {
        if (!IsActive) return items;

        return items.Where(item =>
        {
            var value = _selector(item);
            return (!_minValue.HasValue || value.CompareTo(_minValue.Value) >= 0) &&
                   (!_maxValue.HasValue || value.CompareTo(_maxValue.Value) <= 0);
        });
    }
}

public class StringFilter : FilterBase
{
    private readonly Func<ScannerItem, string?> _selector;
    private readonly string? _value;
    private readonly string[] _ignoreValues;

    public StringFilter(Func<ScannerItem, string?> selector, string? value, params string[] ignoreValues)
    {
        _selector = selector;
        _value = value;
        _ignoreValues = ignoreValues;
    }

    public override bool IsActive => !string.IsNullOrEmpty(_value) && !_ignoreValues.Contains(_value);

    public override IEnumerable<ScannerItem> Apply(IEnumerable<ScannerItem> items)
    {
        if (!IsActive) return items;

        return items.Where(item => _selector(item) == _value);
    }
}

public class ThresholdFilter : FilterBase
{
    private readonly Func<ScannerItem, decimal> _selector;
    private readonly decimal _threshold;

    public ThresholdFilter(Func<ScannerItem, decimal> selector, decimal threshold)
    {
        _selector = selector;
        _threshold = threshold;
    }

    public override bool IsActive => _threshold > 0;

    public override IEnumerable<ScannerItem> Apply(IEnumerable<ScannerItem> items)
    {
        if (!IsActive) return items;

        return items.Where(item => _selector(item) >= _threshold);
    }
}

public class TopNFilter : FilterBase
{
    private readonly int _count;
    private readonly Func<ScannerItem, decimal> _orderBy;

    public TopNFilter(int count, Func<ScannerItem, decimal> orderBy)
    {
        _count = count;
        _orderBy = orderBy;
    }

    public override bool IsActive => _count > 0;

    public override IEnumerable<ScannerItem> Apply(IEnumerable<ScannerItem> items)
    {
        if (!IsActive) return items;

        return items.OrderByDescending(_orderBy).Take(_count);
    }
}
