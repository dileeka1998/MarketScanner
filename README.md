# MarketScanner

A production-ready stock market scanner built with .NET 8 MAUI, featuring an IBKR-style dark theme interface with real-time market data filtering and scanning capabilities.

## Features

- **Dark Theme UI**: IBKR-style dark interface with compact, high-contrast design
- **Real-time Market Data**: Live market scanning with configurable refresh intervals
- **Advanced Filtering**: Comprehensive filter panel with region, sector, exchange, and price filters
- **Sortable Columns**: Click-to-sort functionality on all data columns
- **Auto-refresh**: Configurable automatic data refresh (30-second default)
- **Secure Configuration**: API keys stored in SecureStorage with fallback to environment variables
- **Cross-platform**: Runs on Windows, macOS, iOS, and Android

## Security Features

- **Secure API Key Storage**: Uses MAUI SecureStorage for sensitive configuration
- **HTTPS Only**: All network communication uses encrypted HTTPS
- **Proxy Architecture**: IBKR access through secure backend proxy (never direct API calls)
- **Input Validation**: Comprehensive validation and sanitization of all user inputs
- **Redacted Logging**: Sensitive data is automatically redacted from logs

## Project Structure

```
MarketScanner/
├── Models/                 # Data models
│   ├── ScannerItem.cs
│   └── ScannerFilters.cs
├── ViewModels/             # MVVM ViewModels
│   └── ScannerViewModel.cs
├── Views/                  # UI Pages
│   ├── ScannerPage.xaml
│   └── ScannerPage.xaml.cs
├── Services/               # Business logic services
│   ├── IMarketDataService.cs
│   ├── IFundamentalsService.cs
│   ├── SettingsService.cs
│   └── Impl/
│       ├── MarketDataService.cs
│       └── FundamentalsService.cs
├── Converters/             # Value converters
│   ├── ChangeToColorConverter.cs
│   ├── StringToBoolConverter.cs
│   └── InvertedBoolConverter.cs
├── Theme/                  # UI styling
│   ├── Colors.xaml
│   └── Styles.xaml
├── Utilities/              # Helper utilities
│   ├── Sorting.cs
│   └── Debounce.cs
├── Config/                 # Configuration
│   └── AppSettings.cs
└── appsettings.json        # Configuration file
```

## Setup Instructions

### 1. Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or Visual Studio Code with C# extension
- Platform-specific development tools (Xcode for iOS, Android SDK for Android)

### 2. Configuration

#### API Keys and Backend Configuration

1. **Update `appsettings.json`**:

   ```json
   {
     "IbkrProxyBaseUrl": "https://your-secure-proxy-server.com/api",
     "ApiKey": "YOUR_API_KEY_HERE",
     "RefreshIntervalSeconds": 30,
     "DebounceMilliseconds": 500,
     "EnableVerboseLogging": false
   }
   ```

2. **Set up SecureStorage** (Recommended):

   - Use the app's settings to store API keys securely
   - The app will automatically use SecureStorage values if available
   - Fallback to `appsettings.json` if SecureStorage is empty

3. **Environment Variables** (Alternative):
   ```bash
   export IBKR_PROXY_BASE_URL="https://your-proxy-server.com/api"
   export IBKR_API_KEY="your-api-key"
   ```

### 3. Backend Proxy Setup

The app requires a secure backend proxy to access IBKR APIs. Your proxy should:

- Accept POST requests to `/scanner` endpoint
- Accept GET requests to `/fundamentals/{symbol}/float` and `/fundamentals/{symbol}/week52high`
- Use the `X-API-Key` header for authentication
- Return JSON responses matching the expected data models

#### Example Proxy Endpoints

**POST /scanner**

```json
{
  "region": "United States",
  "product": "Stocks",
  "sector": "Technology",
  "exchange": "NASDAQ",
  "minPrice": 10.0,
  "minChangePercent": 5.0,
  "minRelVolume": 2.0,
  "topN": 25
}
```

**Response:**

```json
[
  {
    "symbol": "AAPL",
    "company": "Apple Inc.",
    "lastPrice": 150.25,
    "change": 2.15,
    "changePercent": 1.45,
    "relativeVolume": 1.8,
    "floatShares": 15400000000,
    "week52High": 182.94
  }
]
```

### 4. Build and Run

#### Windows

```bash
dotnet build -f net8.0-windows10.0.19041.0
dotnet run -f net8.0-windows10.0.19041.0
```

#### macOS

```bash
dotnet build -f net8.0-maccatalyst
dotnet run -f net8.0-maccatalyst
```

#### iOS (requires macOS)

```bash
dotnet build -f net8.0-ios
# Deploy to simulator or device via Visual Studio or Xcode
```

#### Android

```bash
dotnet build -f net8.0-android
# Deploy to emulator or device via Visual Studio or Android Studio
```

### 5. Usage

1. **Configure Filters**: Use the right panel to set up your scanning criteria
2. **Auto-refresh**: Enable auto-refresh for real-time updates (30-second intervals)
3. **Manual Refresh**: Click the Refresh button for immediate updates
4. **Sorting**: Click column headers to sort data
5. **Filter Changes**: Filters automatically trigger new scans with debouncing

## Dependencies

- **CommunityToolkit.MVVM**: MVVM framework with source generators
- **Refit**: HTTP client library for API calls
- **Microsoft.Extensions.Http**: HTTP client factory
- **Microsoft.Extensions.Configuration**: Configuration management

## Security Considerations

1. **Never commit API keys** to version control
2. **Use SecureStorage** for production API keys
3. **Implement proper authentication** on your backend proxy
4. **Use HTTPS** for all network communication
5. **Validate and sanitize** all user inputs
6. **Implement rate limiting** on your backend proxy

## Troubleshooting

### Common Issues

1. **No data loading**: Check your proxy server configuration and API key
2. **Connection errors**: Verify network connectivity and proxy URL
3. **Build errors**: Ensure all NuGet packages are restored
4. **Platform-specific issues**: Check platform-specific requirements and permissions

### Debug Mode

Enable verbose logging in `appsettings.json`:

```json
{
  "EnableVerboseLogging": true
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

This software is for educational and informational purposes only. It is not affiliated with Interactive Brokers LLC. Always verify data accuracy and consult with financial professionals before making investment decisions.
