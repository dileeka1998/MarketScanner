using Microsoft.Maui.Controls;
using MarketScanner.Services;

namespace MarketScanner.Behaviors;

public class HeaderAutoSizeBehavior : Behavior<Label>
{
    public static readonly BindableProperty ColumnIndexProperty = 
        BindableProperty.Create(nameof(ColumnIndex), typeof(int), typeof(HeaderAutoSizeBehavior), -1);
    
    public static readonly BindableProperty LayoutProperty = 
        BindableProperty.Create(nameof(Layout), typeof(ColumnLayoutService), typeof(HeaderAutoSizeBehavior));
    
    public static readonly BindableProperty HeaderGridProperty = 
        BindableProperty.Create(nameof(HeaderGrid), typeof(Grid), typeof(HeaderAutoSizeBehavior));
    
    public static readonly BindableProperty RowsProperty = 
        BindableProperty.Create(nameof(Rows), typeof(CollectionView), typeof(HeaderAutoSizeBehavior));

    public int ColumnIndex
    {
        get => (int)GetValue(ColumnIndexProperty);
        set => SetValue(ColumnIndexProperty, value);
    }

    public ColumnLayoutService? Layout
    {
        get => (ColumnLayoutService?)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public Grid? HeaderGrid
    {
        get => (Grid?)GetValue(HeaderGridProperty);
        set => SetValue(HeaderGridProperty, value);
    }

    public CollectionView? Rows
    {
        get => (CollectionView?)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    private TapGestureRecognizer? _tapGesture;

    protected override void OnAttachedTo(Label bindable)
    {
        base.OnAttachedTo(bindable);
        
        _tapGesture = new TapGestureRecognizer
        {
            NumberOfTapsRequired = 2
        };
        _tapGesture.Tapped += OnDoubleTapped;
        bindable.GestureRecognizers.Add(_tapGesture);
    }

    protected override void OnDetachingFrom(Label bindable)
    {
        base.OnDetachingFrom(bindable);
        
        if (_tapGesture != null)
        {
            _tapGesture.Tapped -= OnDoubleTapped;
            bindable.GestureRecognizers.Remove(_tapGesture);
            _tapGesture = null;
        }
    }

    private async void OnDoubleTapped(object? sender, EventArgs e)
    {
        if (Layout == null || HeaderGrid == null || Rows == null || ColumnIndex < 0)
            return;

        try
        {
            // Collect visible row grids
            var sampleRows = GetVisibleRowGrids();
            
            // Auto-size the column
            await Layout.AutoSizeAsync(ColumnIndex, HeaderGrid, sampleRows);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-size failed: {ex.Message}");
        }
    }

    private IEnumerable<Grid> GetVisibleRowGrids()
    {
        if (Rows == null) yield break;

        // Try to get visible row grids from the CollectionView
        var visibleViews = new List<View>();
        
        // This is a simplified approach - in a real implementation, you might need
        // to use platform-specific code to get the actual visible items
        try
        {
            // For now, we'll return an empty collection and rely on header measurement
            // In a production app, you'd implement proper visible item detection
        }
        catch
        {
            // Fallback to empty collection
        }

        foreach (var view in visibleViews)
        {
            if (view is Grid grid && view.FindByName<Grid>("RowGrid") is Grid rowGrid)
            {
                yield return rowGrid;
            }
        }
    }
}
