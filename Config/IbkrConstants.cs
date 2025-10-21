namespace MarketScanner.Config;

/// <summary>
/// IBKR constants following recommended architecture
/// </summary>
public static class IbkrConstants
{
    // Scanner location codes
    public const string US_STOCKS_MAJOR = "STK.US.MAJOR";
    public const string US_STOCKS = "STK.US";
    public const string EUROPE_STOCKS = "STK.EU";
    public const string ASIA_STOCKS = "STK.HK";  // Fixed from "STK.ASIA" (which doesn't exist)
    
    // Scanner setting pairs
    public const string TOP_PERC_GAIN = "TOP_PERC_GAIN";
    public const string TOP_PERC_LOSS = "TOP_PERC_LOSS";
    public const string TOP_VOLUME = "TOP_VOLUME";
    public const string TOP_TRADE_COUNT = "TOP_TRADE_COUNT";
    
    // Contract types
    public const string STOCK = "STK";
    public const string OPTION = "OPT";
    public const string FUTURE = "FUT";
    
    // Exchanges
    public const string SMART = "SMART";
    public const string NASDAQ = "NASDAQ";
    public const string NYSE = "NYSE";
    public const string AMEX = "AMEX";
    public const string OTC = "OTC";
    
    // Currencies
    public const string USD = "USD";
    public const string EUR = "EUR";
    public const string GBP = "GBP";
    
    // Tick types for market data
    public const string MARKET_DATA_TICKS = "233,165,375"; // Last, Volume, RT Volume
    
    // Historical data
    public const string HISTORICAL_TRADES = "TRADES";
    public const string HISTORICAL_MIDPOINT = "MIDPOINT";
    public const string HISTORICAL_BID = "BID";
    public const string HISTORICAL_ASK = "ASK";
    public const string HISTORICAL_BID_ASK = "BID_ASK";
    
    // Bar sizes
    public const string BAR_SIZE_1_SEC = "1 sec";
    public const string BAR_SIZE_5_SEC = "5 secs";
    public const string BAR_SIZE_10_SEC = "10 secs";
    public const string BAR_SIZE_15_SEC = "15 secs";
    public const string BAR_SIZE_30_SEC = "30 secs";
    public const string BAR_SIZE_1_MIN = "1 min";
    public const string BAR_SIZE_2_MIN = "2 mins";
    public const string BAR_SIZE_3_MIN = "3 mins";
    public const string BAR_SIZE_5_MIN = "5 mins";
    public const string BAR_SIZE_10_MIN = "10 mins";
    public const string BAR_SIZE_15_MIN = "15 mins";
    public const string BAR_SIZE_20_MIN = "20 mins";
    public const string BAR_SIZE_30_MIN = "30 mins";
    public const string BAR_SIZE_1_HOUR = "1 hour";
    public const string BAR_SIZE_1_DAY = "1 day";
    
    // Durations
    public const string DURATION_1_DAY = "1 D";
    public const string DURATION_1_WEEK = "1 W";
    public const string DURATION_1_MONTH = "1 M";
    public const string DURATION_3_MONTHS = "3 M";
    public const string DURATION_6_MONTHS = "6 M";
    public const string DURATION_1_YEAR = "1 Y";
    public const string DURATION_2_YEARS = "2 Y";
    
    // Request ID ranges
    public const int MARKET_DATA_START_ID = 1000;
    public const int HISTORICAL_DATA_START_ID = 2000;
    public const int SCANNER_START_ID = 3000;
    public const int FUNDAMENTALS_START_ID = 4000;
    
    // Default values
    public const int DEFAULT_TOP_N = 50;
    public const long DEFAULT_MIN_VOLUME = 100000;
    public const double DEFAULT_PRICE_MIN = 2.0;
    public const double DEFAULT_PRICE_MAX = 20.0;
    public const double DEFAULT_CHANGE_MIN = -50.0;
    public const double DEFAULT_CHANGE_MAX = 50.0;
    
    // Timeouts (in seconds)
    public const int DEFAULT_CONNECTION_TIMEOUT = 30;
    public const int DEFAULT_REQUEST_TIMEOUT = 10;
    public const int DEFAULT_SCANNER_TIMEOUT = 15;
    public const int DEFAULT_MARKET_DATA_START_TIMEOUT = 5;
}
