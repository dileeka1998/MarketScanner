using CommunityToolkit.Mvvm.ComponentModel;

namespace MarketScanner.Models;

public sealed class ScannerFilters : ObservableObject
{
    public const int FilterPrefsVersion = 2; // bump when we change semantics

    private string _region = "United States";
    private string _product = "Stocks";
    private string _sector = "Any";
    private string _exchange = "US Stocks";

    private decimal? _minPrice = null;
    private decimal? _maxPrice = null;
    private decimal? _minChangePercent = null;
    private decimal? _maxChangePercent = null;
    private decimal? _minRelVolume = null;
    private int? _volumeMin = null;

    private int _topN = 100;
    private bool _autoRefresh = false;

    public string Region { get => _region; set { if (SetProperty(ref _region, value)) OnPropertyChanged(nameof(Region)); } }
    public string Product { get => _product; set { if (SetProperty(ref _product, value)) OnPropertyChanged(nameof(Product)); } }
    public string Sector  { get => _sector;  set { if (SetProperty(ref _sector, value)) OnPropertyChanged(nameof(Sector)); } }
    public string Exchange{ get => _exchange;set { if (SetProperty(ref _exchange, value)) OnPropertyChanged(nameof(Exchange)); } }

    public decimal? MinPrice { get => _minPrice; set { if (SetProperty(ref _minPrice, value)) OnPropertyChanged(nameof(MinPrice)); } }
    public decimal? MaxPrice { get => _maxPrice; set { if (SetProperty(ref _maxPrice, value)) OnPropertyChanged(nameof(MaxPrice)); } }
    public decimal? MinChangePercent { get => _minChangePercent; set { if (SetProperty(ref _minChangePercent, value)) OnPropertyChanged(nameof(MinChangePercent)); } }
    public decimal? MaxChangePercent { get => _maxChangePercent; set { if (SetProperty(ref _maxChangePercent, value)) OnPropertyChanged(nameof(MaxChangePercent)); } }
    public decimal? MinRelVolume { get => _minRelVolume; set { if (SetProperty(ref _minRelVolume, value)) OnPropertyChanged(nameof(MinRelVolume)); } }
    public int? VolumeMin { get => _volumeMin; set { if (SetProperty(ref _volumeMin, value)) OnPropertyChanged(nameof(VolumeMin)); } }

    public int TopN { get => _topN; set { if (SetProperty(ref _topN, value)) OnPropertyChanged(nameof(TopN)); } }
    public bool AutoRefresh { get => _autoRefresh; set { if (SetProperty(ref _autoRefresh, value)) OnPropertyChanged(nameof(AutoRefresh)); } }

    public void Reset()
    {
        Region = "United States";
        Product = "Stocks";
        Sector = "Any";
        Exchange = "US Stocks";
        MinPrice = MaxPrice = null;
        MinChangePercent = MaxChangePercent = null;
        MinRelVolume = null;
        VolumeMin = null;
        TopN = 100;
        AutoRefresh = false;
    }

    public event Action? FilterChanged;

    private new void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);
        FilterChanged?.Invoke();
    }

}
