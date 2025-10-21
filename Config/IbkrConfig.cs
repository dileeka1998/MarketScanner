namespace MarketScanner.Config;

/// <summary>
/// IBKR configuration settings following recommended architecture
/// </summary>
public class IbkrConfig
{
    public bool Enabled { get; set; } = true;
    public bool PreferLive { get; set; } = false;
    public bool UseDelayedData { get; set; } = true;
    public int MarketDataType { get; set; } = 3; // 1: Live, 2: Frozen, 3: Delayed, 4: Delayed Frozen
    public int HistoricalDataDays { get; set; } = 30;
    public int LiveSnapshotMs { get; set; } = 3000;
    public int DelayedSnapshotMs { get; set; } = 4000;
    public double MinLiveHitRatio { get; set; } = 0.0;
    public bool UseStreamingOnDelayed { get; set; } = true;
    public bool EnrichFundamentals { get; set; } = false;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4002;
    public int ClientId { get; set; } = 7777;
    public int ConnectionTimeout { get; set; } = 30;
    public int RequestTimeout { get; set; } = 10;
    
    public IbkrTicksConfig Ticks { get; set; } = new();
    public IbkrTimeoutsConfig Timeouts { get; set; } = new();
    public IbkrScannerDefaultsConfig ScannerDefaults { get; set; } = new();
}

public class IbkrTicksConfig
{
    public int LastPrice { get; set; } = 1;
    public int Bid { get; set; } = 2;
    public int Ask { get; set; } = 3;
    public int High { get; set; } = 4;
    public int Low { get; set; } = 5;
    public int Close { get; set; } = 6;
    public int Open { get; set; } = 7;
    public int Volume { get; set; } = 8;
    public int ClosePrice { get; set; } = 9;
    public int AvgVolume { get; set; } = 21;
}

public class IbkrTimeoutsConfig
{
    public int MarketDataStartSeconds { get; set; } = 5;
    public int FundamentalsSeconds { get; set; } = 8;
    public int Connection { get; set; } = 30;
    public int Request { get; set; } = 10;
    public int Scanner { get; set; } = 15;
}

public class IbkrScannerDefaultsConfig
{
    public string Region { get; set; } = "us";
    public string Product { get; set; } = "stocks";
    public string Sector { get; set; } = "any";
    public string Exchange { get; set; } = "us stocks";
    public int TopN { get; set; } = 50;
    public long MinVolume { get; set; } = 100000;
    public double PriceMin { get; set; } = 2.0;
    public double PriceMax { get; set; } = 20.0;
    public double MinChangePercent { get; set; } = -50.0;
    public double MaxChangePercent { get; set; } = 50.0;
}
