using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MarketScanner.Services;
using MarketScanner.Services.Ibkr;
using MarketScanner.Services.Impl;
using MarketScanner.ViewModels;
using MarketScanner.Views;
using MarketScanner.Config;
using System.Reflection;
using CommunityToolkit.Maui;
using NReco.Logging.File;
using System.IO;

namespace MarketScanner
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Configuration
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("MarketScanner.appsettings.json");
            if (stream != null)
            {
                builder.Configuration.AddJsonStream(stream);
            }

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Services
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<ColumnLayoutService>();
            builder.Services.AddSingleton<IWatchlistService, WatchlistService>();
            
            // Register IBKR configuration
            builder.Services.Configure<IbkrConfig>(builder.Configuration.GetSection("Ibkr"));
            builder.Services.AddSingleton<IbkrConfig>(provider => 
                provider.GetRequiredService<IOptions<IbkrConfig>>().Value);
            
            builder.Services.AddSingleton<AppSettings>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                return new AppSettings
                {
                    IbkrProxyBaseUrl = config["IbkrProxyBaseUrl"] ?? "",
                    ApiKey = config["ApiKey"] ?? "",
                    RefreshIntervalSeconds = int.Parse(config["RefreshIntervalSeconds"] ?? "30"),
                    DebounceMilliseconds = int.Parse(config["DebounceMilliseconds"] ?? "500"),
                    EnableVerboseLogging = bool.Parse(config["EnableVerboseLogging"] ?? "false"),
                    EnableFundamentals = bool.Parse(config["Features:EnableFundamentals"] ?? "true"),
                    AutoStartOnLaunch = bool.Parse(config["AutoStartOnLaunch"] ?? "false")
                };
            });

            // Dispatcher service
            builder.Services.AddSingleton<IDispatcherService, MauiDispatcherService>();

            // IBKR Services - Single unified gateway service
            builder.Services.AddSingleton<IbkrGatewayService>();
            builder.Services.AddSingleton<IScanner>(sp => sp.GetRequiredService<IbkrGatewayService>());
            builder.Services.AddSingleton<IMarketDataService>(sp => sp.GetRequiredService<IbkrGatewayService>());
            builder.Services.AddSingleton<IInstrumentMetadataProvider, IbkrInstrumentMetadataProvider>();

            // MAUI Services
            builder.Services.AddSingleton<IConnectivity>(provider => 
                Microsoft.Maui.Networking.Connectivity.Current);

            // ViewModels
            builder.Services.AddTransient<ScannerViewModel>();
            // WatchlistViewModel is created on-demand by ScannerViewModel

            // Views
            builder.Services.AddTransient<ScannerPage>();

            // Logging
            var logsDir = ResolveLogsDir();
            Console.WriteLine($"[Logging] Writing to: {logsDir}");

            builder.Logging
                .AddConsole()
                .AddDebug()
                .AddFile(Path.Combine(logsDir, "app-{Date}.log"), opts =>
                {
#if DEBUG
                    opts.MinLevel = LogLevel.Trace;
#else
                    opts.MinLevel = LogLevel.Information;
#endif
                    opts.MaxRollingFiles = 7;
                    opts.FileSizeLimitBytes = 10_000_000;
                    opts.Append = true;
                });

            return builder.Build();
        }

        private static string ResolveLogsDir()
        {
            // Try to use a logs directory in the project root
            var currentDir = Directory.GetCurrentDirectory();
            var projectDir = currentDir;
            
            // Walk up to find the project directory
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "MarketScanner.csproj")))
            {
                projectDir = Path.GetDirectoryName(projectDir);
            }

            if (!string.IsNullOrEmpty(projectDir))
            {
                var logsDir = Path.Combine(projectDir, "Logs");
                try
                {
                    Directory.CreateDirectory(logsDir);
                    return logsDir;
                }
                catch
                {
                    // Fall through to default
                }
            }

            // Fallback to AppData
            var appData = Path.Combine(FileSystem.AppDataDirectory, "Logs");
            Directory.CreateDirectory(appData);
            return appData;
        }
    }
}
