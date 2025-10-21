using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MarketScanner.Services;

public sealed class ColumnLayoutService
{
    public const int ColumnCount = 10;
    public const double MinWidth = 80;
    public const double MaxWidth = 420;
    
    // Compact defaults: Symbol, Company, Chg%, Chg, Last, RV, Vol, AvgVol, Float, 52W
    private static readonly double[] Defaults = { 110, 220, 110, 110, 120, 90, 140, 140, 110, 120 };
    
    private const string PrefKey = "Scanner.ColWidths";
    
    public ObservableCollection<ColumnDefinition> Columns { get; } = new();
    public event EventHandler? ColumnsChanged;
    
    public ColumnLayoutService()
    {
        // Initialize with default widths
        for (int i = 0; i < ColumnCount; i++)
        {
            Columns.Add(new ColumnDefinition { Width = new GridLength(Defaults[i], GridUnitType.Absolute) });
        }
    }
    
    public double Get(int index)
    {
        if (index >= 0 && index < Columns.Count)
            return Columns[index].Width.Value;
        return Defaults[index];
    }
    
    public void Set(int index, double width)
    {
        if (index < 0 || index >= Columns.Count) return;
        
        var clamped = Math.Clamp(width, MinWidth, MaxWidth);
        var current = Columns[index].Width.Value;
        
        if (Math.Abs(current - clamped) < 0.5) return;
        
        Columns[index].Width = new GridLength(clamped, GridUnitType.Absolute);
        ColumnsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void Load()
    {
        var saved = Extensions.PreferencesExtensions.GetDoubleArray(PrefKey, Defaults);
        
        for (int i = 0; i < Math.Min(ColumnCount, saved.Length); i++)
        {
            Columns[i].Width = new GridLength(saved[i], GridUnitType.Absolute);
        }
        
        ColumnsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void Save()
    {
        var widths = new double[ColumnCount];
        for (int i = 0; i < ColumnCount; i++)
        {
            widths[i] = Columns[i].Width.Value;
        }
        Extensions.PreferencesExtensions.SetDoubleArray(PrefKey, widths);
    }
    
    public void SetDefaults()
    {
        for (int i = 0; i < ColumnCount; i++)
        {
            Columns[i].Width = new GridLength(Defaults[i], GridUnitType.Absolute);
        }
        ColumnsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public Task AutoSizeAsync(int columnIndex, Grid headerGrid, IEnumerable<Grid> sampleRows)
    {
        if (columnIndex < 0 || columnIndex >= ColumnCount) return Task.CompletedTask;
        
        var maxWidth = 0.0;
        
        // Measure header cell
        var headerLabel = GetHeaderLabel(headerGrid, columnIndex);
        if (headerLabel != null)
        {
            var headerSize = headerLabel.Measure(double.PositiveInfinity, double.PositiveInfinity);
            maxWidth = Math.Max(maxWidth, headerSize.Request.Width);
        }
        
        // Measure sample row cells
        var rowCount = 0;
        foreach (var rowGrid in sampleRows.Take(20))
        {
            var rowLabel = GetRowLabel(rowGrid, columnIndex);
            if (rowLabel != null)
            {
                var rowSize = rowLabel.Measure(double.PositiveInfinity, double.PositiveInfinity);
                maxWidth = Math.Max(maxWidth, rowSize.Request.Width);
                rowCount++;
            }
        }
        
        // Add padding and set width
        var desiredWidth = maxWidth + 18; // 18px padding
        Set(columnIndex, desiredWidth);
        Save();
        
        return Task.CompletedTask;
    }
    
    private Label? GetHeaderLabel(Grid headerGrid, int columnIndex)
    {
        foreach (var child in headerGrid.Children)
        {
            if (child is Label label && Grid.GetColumn(label) == columnIndex)
            {
                return label;
            }
        }
        return null;
    }
    
    private Label? GetRowLabel(Grid rowGrid, int columnIndex)
    {
        foreach (var child in rowGrid.Children)
        {
            if (child is Label label && Grid.GetColumn(label) == columnIndex)
            {
                return label;
            }
        }
        return null;
    }
}
