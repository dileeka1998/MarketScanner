using Microsoft.Maui.Controls;
using MarketScanner.Services;

namespace MarketScanner.Utilities;

public class ColumnResizeBehavior : Behavior<View>
{
    public static readonly BindableProperty ColumnIndexProperty = 
        BindableProperty.Create(nameof(ColumnIndex), typeof(int), typeof(ColumnResizeBehavior), -1);
    
    public static readonly BindableProperty HostScrollViewProperty = 
        BindableProperty.Create(nameof(HostScrollView), typeof(ScrollView), typeof(ColumnResizeBehavior));
    
    public static readonly BindableProperty LayoutProperty = 
        BindableProperty.Create(nameof(Layout), typeof(ColumnLayoutService), typeof(ColumnResizeBehavior));

    public int ColumnIndex
    {
        get => (int)GetValue(ColumnIndexProperty);
        set => SetValue(ColumnIndexProperty, value);
    }

    public ScrollView? HostScrollView
    {
        get => (ScrollView?)GetValue(HostScrollViewProperty);
        set => SetValue(HostScrollViewProperty, value);
    }

    public ColumnLayoutService? Layout
    {
        get => (ColumnLayoutService?)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    private double _startWidth;
    private bool _isDragging;
    private PanGestureRecognizer? _panGesture;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        
        _panGesture = new PanGestureRecognizer();
        _panGesture.PanUpdated += OnPanUpdated;
        bindable.GestureRecognizers.Add(_panGesture);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        base.OnDetachingFrom(bindable);
        
        if (_panGesture != null)
        {
            _panGesture.PanUpdated -= OnPanUpdated;
            bindable.GestureRecognizers.Remove(_panGesture);
            _panGesture = null;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (Layout == null || ColumnIndex < 0)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging = true;
                _startWidth = Layout.Get(ColumnIndex);
                
                // Disable horizontal scrolling during drag
                if (HostScrollView != null)
                {
                    HostScrollView.IsEnabled = false;
                }
                break;
                
            case GestureStatus.Running:
                if (_isDragging)
                {
                    var newWidth = _startWidth + e.TotalX;
                    Layout.Set(ColumnIndex, newWidth);
                }
                break;
                
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isDragging = false;
                
                // Re-enable horizontal scrolling
                if (HostScrollView != null)
                {
                    HostScrollView.IsEnabled = true;
                }
                
                // Save widths
                Layout.Save();
                break;
        }
    }
}