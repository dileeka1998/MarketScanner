namespace MarketScanner.Config;

public class AppSettings
{
    public string IbkrProxyBaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int DebounceMilliseconds { get; set; } = 500;
    public bool EnableVerboseLogging { get; set; } = false;
    public bool EnableFundamentals { get; set; } = true;
    public bool AutoStartOnLaunch { get; set; } = false;
}
