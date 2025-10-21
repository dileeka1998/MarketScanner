using System.ComponentModel;
using MarketScanner.Models;
using MarketScanner.Utilities;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MarketScanner.ViewModels;

public sealed class ScannerRowViewModel : ObservableObject, IDisposable
{
    // Displayed values
    private double _lastPrice;
    private double _prevClose;
    private long _volume;
    private long _avgVolume;
    private RowStatus _status;
    private FundamentalsStatus _fundamentalsStatus;
    private bool _hasMarketData;
    
    private readonly ILogger? _logger;

    private string _symbol = string.Empty;
    private string _company = string.Empty;

    public string Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }
    public int Rank { get; set; }
    public string Company { get => _company; set => SetProperty(ref _company, value); }
    public string Region { get; set; } = "United States";
    public string Product { get; set; } = "Stocks";
    public string Exchange { get; set; } = "US Stocks";

    public ScannerRowViewModel(ILogger? logger = null)
    {
        _logger = logger;
        _status = RowStatus.PendingEnrichment;
        _fundamentalsStatus = FundamentalsStatus.Skipped;
    }

    public double LastPrice 
    { 
        get => _lastPrice; 
        set 
        { 
            if (SetProperty(ref _lastPrice, value)) 
            { 
                OnPropertyChanged(nameof(DisplayLastPrice)); 
                OnPropertyChanged(nameof(Change)); 
                OnPropertyChanged(nameof(ChangePercent)); 
                OnPropertyChanged(nameof(DisplayChange)); 
                OnPropertyChanged(nameof(DisplayChangePercent)); 
            } 
        } 
    }

    public double PrevClose 
    { 
        get => _prevClose; 
        set 
        { 
            if (SetProperty(ref _prevClose, value)) 
            { 
                OnPropertyChanged(nameof(Change)); 
                OnPropertyChanged(nameof(ChangePercent)); 
                OnPropertyChanged(nameof(DisplayChange)); 
                OnPropertyChanged(nameof(DisplayChangePercent)); 
            } 
        } 
    }

    public long Volume 
    { 
        get => _volume; 
        set 
        { 
            if (SetProperty(ref _volume, value)) 
            { 
                OnPropertyChanged(nameof(DisplayVolume)); 
                OnPropertyChanged(nameof(RelativeVolume)); 
                OnPropertyChanged(nameof(DisplayRV)); 
            } 
        } 
    }

    public long AvgVolume 
    { 
        get => _avgVolume; 
        set 
        { 
            if (SetProperty(ref _avgVolume, value)) 
            { 
                OnPropertyChanged(nameof(DisplayAvgVolume)); 
                OnPropertyChanged(nameof(RelativeVolume)); 
                OnPropertyChanged(nameof(DisplayRV)); 
            } 
        } 
    }

    private bool HasBoth => LastPrice > 0 && PrevClose > 0;

    public double Change => HasBoth ? (LastPrice - PrevClose) : 0d;

    public double ChangePercent => (HasBoth && PrevClose != 0)
        ? (Change / PrevClose) * 100.0
        : 0d;



    public double RelativeVolume => VolumeCalculations.CalculateRelativeVolume(Volume, AvgVolume);

    // Display helpers (blank until both exist)
    public string DisplayLastPrice => LastPrice <= 0 ? "$0.00" : $"${LastPrice:0.00}";
    public string DisplayChange => HasBoth ? Change.ToString("+0.00;-0.00;0.00") : string.Empty;
    public string DisplayChangePercent => HasBoth ? $"{ChangePercent:+0.00;-0.00;0.00}%" : string.Empty;
    public string DisplayVolume => Volume.ToString("N0");
    public string DisplayAvgVolume => AvgVolume.ToString("N0");
    public string DisplayRV => $"{RelativeVolume:0.00}×";

    // Removed FloatShares and FiftyTwoWeekHigh properties as requested

    public RowStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public FundamentalsStatus FundamentalsStatus
    {
        get => _fundamentalsStatus;
        set { _fundamentalsStatus = value; OnPropertyChanged(); }
    }

    public bool HasMarketData
    {
        get => _hasMarketData;
        set { _hasMarketData = value; OnPropertyChanged(); }
    }


    // Debounced update methods for high-frequency tick data
    public void UpdateLastPrice(double value)
    {
        if (_lastPrice == value) return; // Skip if no change
        
        MainThread.BeginInvokeOnMainThread(() => LastPrice = value);
    }

    public void UpdateVolume(long value)
    {
        if (_volume == value) return; // Skip if no change
        
        MainThread.BeginInvokeOnMainThread(() => Volume = value);
    }

    public void UpdateClosePrice(double closePrice)
    {
        if (_prevClose == closePrice) return; // Skip if no change
        
        MainThread.BeginInvokeOnMainThread(() => PrevClose = closePrice);
    }


    // Direct property setters for non-tick data (enrichment, status, etc.)
    public void SetAvgVolume(long value)
    {
        // Don't skip if current value is 0 - we want to update from 0 to actual value
        if (_avgVolume == value && _avgVolume != 0) return;
        MainThread.BeginInvokeOnMainThread(() => AvgVolume = value);
    }

    // Removed SetFloatShares and SetFiftyTwoWeekHigh methods as requested

    public void SetStatus(RowStatus value)
    {
        if (_status != value)
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    public void SetFundamentalsStatus(FundamentalsStatus value)
    {
        if (_fundamentalsStatus != value)
        {
            _fundamentalsStatus = value;
            OnPropertyChanged(nameof(FundamentalsStatus));
        }
    }

    public void SetHasMarketData(bool value)
    {
        if (_hasMarketData != value)
        {
            _hasMarketData = value;
            OnPropertyChanged(nameof(HasMarketData));
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
