using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MarketScanner.Utilities;

public sealed class ColumnWidthManager
{
    public const int Count = 10;
    private const string PrefKey = "Scanner.ColumnWidths";
    private static readonly double[] Defaults = { 140, 220, 120, 110, 120, 100, 130, 130, 110, 120 };
    private readonly double[] _widths = new double[Count];

    public static ObservableCollection<ColumnDefinition> SharedColumns { get; } = new();

    static ColumnWidthManager()
    {
        // Initialize shared columns with default widths
        for (int i = 0; i < Count; i++)
        {
            SharedColumns.Add(new ColumnDefinition { Width = new GridLength(Defaults[i], GridUnitType.Absolute) });
        }
    }

    public ColumnWidthManager()
    {
        Array.Copy(Defaults, _widths, Count);
    }

    public double Get(int index) => _widths[index];

    public void Set(int index, double value)
    {
        var clamped = Math.Clamp(value, 80, 420);
        if (Math.Abs(_widths[index] - clamped) < 0.5) return;
        _widths[index] = clamped;
        
        // Update shared column definition directly
        if (index >= 0 && index < SharedColumns.Count)
        {
            SharedColumns[index].Width = new GridLength(clamped, GridUnitType.Absolute);
        }
    }

    public void Load()
    {
        var loaded = Extensions.PreferencesExtensions.GetDoubleArray(PrefKey, Defaults);
        for (int i = 0; i < Count; i++) 
        {
            _widths[i] = loaded[i];
            // Update shared column definition
            if (i < SharedColumns.Count)
            {
                SharedColumns[i].Width = new GridLength(loaded[i], GridUnitType.Absolute);
            }
        }
    }

    public void Save()
    {
        // Update _widths from current SharedColumns values
        for (int i = 0; i < Math.Min(Count, SharedColumns.Count); i++)
        {
            _widths[i] = SharedColumns[i].Width.Value;
        }
        Extensions.PreferencesExtensions.SetDoubleArray(PrefKey, _widths);
    }

    public double[] Snapshot() => (double[])_widths.Clone();
}