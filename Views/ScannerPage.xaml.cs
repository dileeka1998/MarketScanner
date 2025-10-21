using MarketScanner.ViewModels;
using MarketScanner.Services;
using MarketScanner.Utilities;
using MarketScanner.Behaviors;

namespace MarketScanner.Views;

public partial class ScannerPage : ContentPage
{
    private readonly ColumnLayoutService _layout;

    public ScannerPage(ColumnLayoutService layout)
    {
        _layout = layout;
        _layout.Load();
        InitializeComponent();
        
        // Set up shared column definitions
        SetupSharedColumns();
        
        // Assign behavior properties
        AssignBehaviorProperties();
        
        // Set up CollectionView with shared columns
        SetupCollectionView();
    }

    public ScannerPage(ScannerViewModel viewModel, ColumnLayoutService layout) : this(layout)
    {
        BindingContext = viewModel;
    }

    private void SetupSharedColumns()
    {
        if (HeaderGrid == null) return;
        
        // Clear existing column definitions
        HeaderGrid.ColumnDefinitions.Clear();
        
        // Add shared column definitions to header grid
        foreach (var colDef in _layout.Columns)
        {
            HeaderGrid.ColumnDefinitions.Add(colDef);
        }
    }

    private void AssignBehaviorProperties()
    {
        // Assign properties to resize behaviors
        var grips = new[] { 
            GetGrip(0), GetGrip(1), GetGrip(2), GetGrip(3), GetGrip(4),
            GetGrip(5), GetGrip(6), GetGrip(7)
        };
        
        for (int i = 0; i < grips.Length; i++)
        {
            var grip = grips[i];
            if (grip != null)
            {
                var behavior = grip.Behaviors.OfType<ColumnResizeBehavior>().FirstOrDefault();
                if (behavior != null)
                {
                    behavior.HostScrollView = TableScroll;
                    behavior.Layout = _layout;
                }
            }
        }
        
        // Assign properties to auto-size behaviors
        var headers = new[] {
            GetHeader(0), GetHeader(1), GetHeader(2), GetHeader(3), GetHeader(4),
            GetHeader(5), GetHeader(6), GetHeader(7)
        };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            if (header != null)
            {
                var behavior = header.Behaviors.OfType<HeaderAutoSizeBehavior>().FirstOrDefault();
                if (behavior != null)
                {
                    behavior.Layout = _layout;
                    behavior.HeaderGrid = HeaderGrid;
                    behavior.Rows = Rows;
                }
            }
        }
    }

    private BoxView? GetGrip(int columnIndex)
    {
        if (HeaderGrid == null) return null;
        
        foreach (var child in HeaderGrid.Children)
        {
            if (child is BoxView boxView && Grid.GetColumn(boxView) == columnIndex)
            {
                return boxView;
            }
        }
        return null;
    }

    private Label? GetHeader(int columnIndex)
    {
        if (HeaderGrid == null) return null;
        
        foreach (var child in HeaderGrid.Children)
        {
            if (child is Label label && Grid.GetColumn(label) == columnIndex)
            {
                return label;
            }
        }
        return null;
    }

    private void SetupCollectionView()
    {
        if (Rows == null) return;
        
        // Create a custom DataTemplate that uses shared columns
        var dataTemplate = new DataTemplate(() =>
        {
            var rowGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#1a1a1a"),
                HeightRequest = 32,
                Padding = new Thickness(0),
                ColumnSpacing = 0,
                RowDefinitions = { new RowDefinition { Height = GridLength.Auto } }
            };
            
            // Row grid will be identified by its position in the CollectionView
            
            // Add the SAME column definitions as header (shared references)
            foreach (var colDef in _layout.Columns)
            {
                rowGrid.ColumnDefinitions.Add(colDef);
            }
            
            // Add labels for each column with inline styles
            var symbolLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };
            var companyLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            var changePercentLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                FontFamily = "Cascadia Mono, Consolas, Menlo",
                HorizontalTextAlignment = TextAlignment.End
            };
            var changeLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                FontFamily = "Cascadia Mono, Consolas, Menlo",
                HorizontalTextAlignment = TextAlignment.End
            };
            var lastPriceLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                FontFamily = "Cascadia Mono, Consolas, Menlo",
                HorizontalTextAlignment = TextAlignment.End
            };
            var relativeVolumeLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                FontFamily = "Cascadia Mono, Consolas, Menlo",
                HorizontalTextAlignment = TextAlignment.End
            };
            var volumeLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                FontFamily = "Cascadia Mono, Consolas, Menlo",
                HorizontalTextAlignment = TextAlignment.End
            };
            var averageVolumeLabel = new Label 
            { 
                FontSize = 13,
                Padding = new Thickness(6, 4),
                TextColor = Color.FromArgb("#e0e0e0"),
                VerticalOptions = LayoutOptions.Center,
                FontFamily = "Cascadia Mono, Consolas, Menlo",
                HorizontalTextAlignment = TextAlignment.End
            };
            
            // Set Grid.Column properties
            Grid.SetColumn(symbolLabel, 0);
            Grid.SetColumn(companyLabel, 1);
            Grid.SetColumn(changePercentLabel, 2);
            Grid.SetColumn(changeLabel, 3);
            Grid.SetColumn(lastPriceLabel, 4);
            Grid.SetColumn(relativeVolumeLabel, 5);
            Grid.SetColumn(volumeLabel, 6);
            Grid.SetColumn(averageVolumeLabel, 7);
            
            // Set bindings
            symbolLabel.SetBinding(Label.TextProperty, "Symbol");
            companyLabel.SetBinding(Label.TextProperty, "Company");
            changePercentLabel.SetBinding(Label.TextProperty, new Binding("ChangePercent", stringFormat: "{0:+#0.00;-#0.00;0.00}%"));
            changeLabel.SetBinding(Label.TextProperty, new Binding("Change", stringFormat: "{0:+#0.00;-#0.00;0.00}"));
            lastPriceLabel.SetBinding(Label.TextProperty, new Binding("LastPrice", stringFormat: "{0:C2}"));
            relativeVolumeLabel.SetBinding(Label.TextProperty, new Binding("RelativeVolume", stringFormat: "{0:0.0}x"));
            volumeLabel.SetBinding(Label.TextProperty, new Binding("Volume", stringFormat: "{0:N0}"));
            averageVolumeLabel.SetBinding(Label.TextProperty, "DisplayAvgVolume");
            
            // Set up color changes for positive/negative values
            changePercentLabel.SetBinding(Label.TextColorProperty, new Binding("ChangePercent", converter: new Converters.ChangeToColorConverter()));
            changeLabel.SetBinding(Label.TextColorProperty, new Binding("Change", converter: new Converters.ChangeToColorConverter()));
            
            rowGrid.Children.Add(symbolLabel);
            rowGrid.Children.Add(companyLabel);
            rowGrid.Children.Add(changePercentLabel);
            rowGrid.Children.Add(changeLabel);
            rowGrid.Children.Add(lastPriceLabel);
            rowGrid.Children.Add(relativeVolumeLabel);
            rowGrid.Children.Add(volumeLabel);
            rowGrid.Children.Add(averageVolumeLabel);
            
            return rowGrid;
        });
        
        Rows.ItemTemplate = dataTemplate;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.ScannerViewModel vm)
        {
            // Load refresh preferences on first appearance
            vm.LoadRefreshPrefs();
            
            if (vm.ScannerItems.Count == 0) await vm.RefreshAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is ScannerViewModel vm)
        {
            vm.Dispose();
        }
    }


    private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is ViewModels.ScannerViewModel vm)
            vm.GetType().GetMethod("DebouncedApply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
              ?.Invoke(vm, null);
    }


}