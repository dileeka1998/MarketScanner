using IBApi;
using MarketScanner.Config;
using MarketScanner.Models;
using MarketScanner.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MarketScanner.Services.Ibkr;

/// <summary>
/// Unified IBKR gateway service that implements EWrapper directly.
/// Combines scanner and market data functionality in a single, maintainable service.
/// Based on proven patterns from backup and working Node.js implementation.
/// </summary>
public sealed class IbkrGatewayService : EWrapper, IScanner, IMarketDataService, IDisposable
{
    // Tick field constants (from working Node.js implementation)
    private const int TICK_LAST = 4;
    private const int TICK_CLOSE = 9;
    private const int TICK_VOLUME = 8;
    private const int TICK_OPEN = 14;
    private const int TICK_HIGH = 6;
    private const int TICK_LOW = 7;
    private const int TICK_DELAYED_LAST = 68;
    private const int TICK_DELAYED_CLOSE = 75;
    private const int TICK_DELAYED_VOLUME = 74;
    private const int TICK_DELAYED_OPEN = 73;
    private const int TICK_DELAYED_HIGH = 69;
    private const int TICK_DELAYED_LOW = 70;

    private readonly ILogger<IbkrGatewayService> _logger;
    private readonly IConfiguration _config;
    
    private EClientSocket _client = default!;
    private EReaderSignal _signal = default!;
    private bool _connected;
    private int _nextValidId;
    private int _nextReqId = 1;
    private bool _disposed;

    // Scanner state
    private readonly ConcurrentDictionary<int, List<ScannerRow>> _scannerBuffers = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<List<ScannerRow>>> _scannerWaiters = new();
    private readonly ConcurrentDictionary<int, ScannerRow> _scannerResults = new();
    private readonly ConcurrentDictionary<int, bool> _scannerRequestIds = new(); // Track all scanner request IDs for Error 162 suppression
    private int _currentScannerId = 0; // Track current active scanner for cancellation

    // Market data state
    private readonly Dictionary<int, string> _idToSymbol = new();
    private readonly ConcurrentDictionary<string, MarketState> _marketState = new();
    private readonly ConcurrentDictionary<string, SnapshotRow> _snapshots = new();
    private readonly ConcurrentDictionary<string, long> _averageVolumes = new();
    
    // Historical data tracking
    private readonly ConcurrentDictionary<int, string> _histReqToSymbol = new();
    private readonly ConcurrentDictionary<int, List<long>> _histVolumes = new();

    // Tick stream for live updates
    private readonly Subject<TickData> _tickSubject = new();
    public IObservable<TickData> TickStream => _tickSubject.AsObservable();

    // Scanner events
    public event Func<ScannerSnapshot, Task>? SnapshotReceived;

    private class MarketState
    {
        public decimal? LastPrice { get; set; }
        public decimal? Open { get; set; }
        public decimal? High { get; set; }
        public decimal? Low { get; set; }
        public decimal? PrevClose { get; set; }
        public long? Volume { get; set; }
        public long? AverageVolume { get; set; }
    }

    public IbkrGatewayService(ILogger<IbkrGatewayService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    #region Connection Management

    public async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_connected) return;

        var host = _config.GetValue<string>("Ibkr:Host") ?? "127.0.0.1";
        var port = _config.GetValue<int?>("Ibkr:Port") ?? 4002;
        var clientId = _config.GetValue<int?>("Ibkr:ClientId") ?? 7777;

        _logger.LogInformation("Connecting to IBKR at {Host}:{Port} with ClientId {ClientId}", 
            host, port, clientId);

        _signal = new EReaderMonitorSignal();
        _client = new EClientSocket(this, _signal);  // Pass 'this' as EWrapper
        _client.eConnect(host, port, clientId);

        var reader = new EReader(_client, _signal);
        reader.Start();
        
        _ = Task.Run(() =>
        {
            while (_client.IsConnected())
            {
                _signal.waitForSignal();
                reader.processMsgs();
            }
        }, ct);

        // Wait for nextValidId callback (confirms connection)
        await WaitUntilAsync(() => _nextValidId > 0, TimeSpan.FromSeconds(5), ct);
        _connected = true;
        
        // CRITICAL: Set to DELAYED immediately after connection
        _client.reqMarketDataType(3); // 3 = DELAYED
        _logger.LogInformation("Connected to IBKR (nextValidId={Id}, mode=DELAYED)", _nextValidId);
        
        // Scanner parameters not needed - we use hardcoded region/product mappings
    }

    private async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        while (!condition() && DateTime.UtcNow - start < timeout && !ct.IsCancellationRequested)
        {
            await Task.Delay(50, ct);
        }
    }

    private int GetNextReqId() => Interlocked.Increment(ref _nextReqId);

    #endregion

    #region IScanner Implementation

    public async Task<IReadOnlyList<ScannerRow>> ScanAsync(CancellationToken ct)
    {
        return await ScanAsync(2, 20, "us", "stocks", "us stocks", 50, ct);
    }

    public async Task<IReadOnlyList<ScannerRow>> ScanAsync(
        decimal minPrice = 2,
        decimal maxPrice = 20,
        string region = "us",
        string product = "stocks",
        string exchange = "us stocks",
        int topN = 50,
        CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        // Cancel any existing scanner before starting new one
        if (_currentScannerId > 0)
        {
            _logger.LogDebug("Cancelling previous scanner reqId={PreviousId}", _currentScannerId);
            _client.cancelScannerSubscription(_currentScannerId);
            _scannerWaiters.TryRemove(_currentScannerId, out _);
            _scannerBuffers.TryRemove(_currentScannerId, out _);
        }

        var requestId = GetNextReqId();
        _currentScannerId = requestId; // Track current scanner for future cancellation
        var tcs = new TaskCompletionSource<List<ScannerRow>>();
        
        _scannerWaiters[requestId] = tcs;
        _scannerBuffers[requestId] = new List<ScannerRow>();
        _scannerRequestIds[requestId] = true; // Track for Error 162 suppression

        // Map region to IBKR location code using constants
        var locationCode = region.ToLowerInvariant() switch
        {
            "us" => IbkrConstants.US_STOCKS_MAJOR,      // "STK.US.MAJOR"
            "europe" => IbkrConstants.EUROPE_STOCKS,    // "STK.EU"
            "asia" => IbkrConstants.ASIA_STOCKS,        // "STK.HK" (fixed!)
            "any" => IbkrConstants.US_STOCKS_MAJOR,     // Default to US for "any"
            _ => IbkrConstants.US_STOCKS_MAJOR
        };

        // Map (region, product) → IBKR instrument type (region-aware mapping)
        var instrument = (region.ToLowerInvariant(), product.ToLowerInvariant()) switch
        {
            ("us", "stocks") => "STK",
            ("europe", "stocks") => "STOCK.EU",
            ("asia", "stocks") => "STOCK.HK",
            ("us", "futures") => "FUT.US",
            ("europe", "futures") => "FUT.EU",
            ("asia", "futures") => "FUT.HK",
            ("us", "etfs") => "ETF.EQ.US",
            _ => "STK"  // Default to US stocks
        };

        _logger.LogInformation("Scanner instrument type: {Instrument} (from region: {Region}, product: {Product})", instrument, region, product);

        var scannerSubscription = new ScannerSubscription
        {
            Instrument = instrument,
            LocationCode = locationCode,
            ScanCode = "TOP_PERC_GAIN",
            NumberOfRows = topN,
            AboveVolume = 100_000
        };

        // Scanner options (empty list - not used)
        var scanOptions = new List<TagValue>();
        
        // Filter options - price, exchange filters applied at IBKR level via TagValue
        var filterOptions = new List<TagValue>
        {
            new TagValue("priceAbove", minPrice.ToString("F2")),
            new TagValue("priceBelow", maxPrice.ToString("F2")),
            new TagValue("volumeAbove", "100000")  // Keep volume filter (even though it's broken)
        };

        // Add exchange filter if specified (not "any" or "us stocks")
        if (!string.IsNullOrWhiteSpace(exchange) && 
            !exchange.Equals("any", StringComparison.OrdinalIgnoreCase) &&
            !exchange.Equals("us stocks", StringComparison.OrdinalIgnoreCase))
        {
            // Map UI values to IBKR exchange codes
            var exchangeCode = exchange.ToUpperInvariant() switch
            {
                "NASDAQ" => "NASDAQ",
                "NYSE" => "NYSE",
                "AMEX" => "AMEX",
                _ => exchange.ToUpperInvariant()
            };
            filterOptions.Add(new TagValue("exchange", exchangeCode));
            _logger.LogInformation("Adding exchange filter: {Exchange}", exchangeCode);
        }

        _logger.LogInformation("Starting scanner subscription reqId={RequestId} with filters: price ${MinPrice}-${MaxPrice}, region={Region}, product={Product}, locationCode={LocationCode}, exchange={Exchange}, volume >100k, topN={TopN}", 
            requestId, minPrice, maxPrice, region, product, locationCode, exchange, topN);
        _client.reqScannerSubscription(requestId, scannerSubscription, scanOptions, filterOptions);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        
        try
        {
            linkedCts.Token.Register(() => tcs.TrySetCanceled());
            var rows = await tcs.Task;
            _logger.LogInformation("Scanner returned {Count} rows", rows.Count);
            
            // Cancel previous market data subscriptions to avoid "Duplicate ticker id" errors
            foreach (var kvp in _idToSymbol.ToList())
            {
                _client.cancelMktData(kvp.Key);
            }
            _idToSymbol.Clear();
            _marketState.Clear();
            
            // Cancel historical data requests
            foreach (var kvp in _histReqToSymbol.ToList())
            {
                _client.cancelHistoricalData(kvp.Key);
            }
            _histReqToSymbol.Clear();
            _histVolumes.Clear();
            
            // Subscribe to market data for all scanner results
            var symbols = rows.Select(r => r.Symbol).ToList();
            SubscribeToMarketData(symbols);
            
            return rows;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Scanner request timed out");
            throw new TimeoutException("Scanner request timed out");
        }
        finally
        {
            _client.cancelScannerSubscription(requestId);
            _scannerWaiters.TryRemove(requestId, out _);
            _scannerBuffers.TryRemove(requestId, out _);
            _scannerRequestIds.TryRemove(requestId, out _); // Clean up Error 162 suppression tracking
            _currentScannerId = 0; // Reset current scanner ID
        }
    }

    public Task CancelScannerAsync()
    {
        if (_currentScannerId > 0)
        {
            _logger.LogInformation("Cancelling current scanner subscription reqId={ScannerId}", _currentScannerId);
            _client.cancelScannerSubscription(_currentScannerId);
            _scannerWaiters.TryRemove(_currentScannerId, out _);
            _scannerBuffers.TryRemove(_currentScannerId, out _);
            _scannerRequestIds.TryRemove(_currentScannerId, out _); // Clean up Error 162 suppression tracking
            _currentScannerId = 0;
        }
        return Task.CompletedTask;
    }

    public async Task StartAsync(
        FilterState filters,
        CancellationToken cancellationToken,
        Guid sessionId,
        Func<TickData, Task> onTick,
        Func<EnrichmentData, Task> onEnrichment)
    {
        await EnsureConnectedAsync(cancellationToken);

        _logger.LogInformation("Starting scanner with session {SessionId}", sessionId);

        var rows = await ScanAsync(cancellationToken);
        
        // Subscribe to market data for live updates
        SubscribeToMarketData(rows.Select(r => r.Symbol));

        var scannerItems = rows.Select(row => new ScannerItem
        {
            Symbol = row.Symbol,
            Company = row.Company,
            LastPrice = row.LastPrice,
            Change = row.Change,
            ChangePercent = row.ChangePct,
            Volume = row.Volume,
            AverageVolume = row.AvgVolume,
            RelativeVolume = row.RelativeVolume,
            Float = row.Float,
            FiftyTwoWeekHigh = row.High52W,
            Sector = row.Meta?.Sector ?? "Unknown",
            Exchange = row.Meta?.Exchange ?? "Unknown",
            Region = row.Meta?.Region ?? "us",
            Product = row.Meta?.Product ?? "stocks"
        }).ToList();

        var snapshot = new ScannerSnapshot(scannerItems);
        if (SnapshotReceived != null)
        {
            await SnapshotReceived.Invoke(snapshot);
        }
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Stopping scanner");
        // Cancel all active subscriptions
        foreach (var tickerId in _idToSymbol.Keys)
        {
            try { _client.cancelMktData(tickerId); } catch { }
        }
        _idToSymbol.Clear();
        _marketState.Clear();
        return Task.CompletedTask;
    }

    #endregion

    #region IMarketDataService Implementation

    public async Task<IReadOnlyList<SnapshotRow>> GetSnapshotsAsync(UniverseRequest request, CancellationToken ct)
    {
        await EnsureConnectedAsync(ct);

        var rows = await ScanAsync(ct);
        
        // Subscribe to market data
        SubscribeToMarketData(rows.Select(r => r.Symbol));

        // Wait a bit for ticks to arrive
        await Task.Delay(2000, ct);

        // Convert to snapshots
        return rows.Select(row => new SnapshotRow
        {
            Symbol = row.Symbol,
            Company = row.Company,
            LastPrice = row.LastPrice,
            Change = row.Change,
            ChangePercent = row.ChangePct,
            Volume = row.Volume,
            AverageVolume = _averageVolumes.GetValueOrDefault(row.Symbol, 0),
            RelativeVolume = (decimal)VolumeCalculations.CalculateRelativeVolume(row.Volume, _averageVolumes.GetValueOrDefault(row.Symbol, 0)),
            Float = row.Float,
            FiftyTwoWeekHigh = row.High52W,
            Sector = row.Meta?.Sector ?? "Unknown",
            Exchange = row.Meta?.Exchange ?? "Unknown",
            Region = row.Meta?.Region ?? "us",
            Product = row.Meta?.Product ?? "stocks",
            Timestamp = DateTime.UtcNow
        }).ToList();
    }


    #endregion

    #region Market Data Subscription

    private void SubscribeToMarketData(IEnumerable<string> symbols)
    {
        var tickerId = 10000; // Start from high ID like Node.js
        
        foreach (var symbol in symbols)
        {
            if (_idToSymbol.ContainsValue(symbol)) continue; // Already subscribed

            _idToSymbol[tickerId] = symbol;
            _marketState[symbol] = new MarketState();
            
            var contract = new Contract
            {
                Symbol = symbol,
                SecType = "STK",
                Exchange = "SMART",
                Currency = "USD"
            };
            
            _client.reqMktData(tickerId, contract, "", false, false, null);
            
            // Request historical data for average volume
            RequestHistoricalData(symbol, contract);
            
            tickerId++;
        }
    }

    private void RequestHistoricalData(string symbol, Contract contract)
    {
        var reqId = GetNextReqId();
        _histReqToSymbol[reqId] = symbol;
        _histVolumes[reqId] = new List<long>();
        
        _client.reqHistoricalData(
            reqId, contract, "", "30 D", "1 day", "TRADES", 1, 1, false, null);
    }

    #endregion

    #region EWrapper Callbacks

    public void nextValidId(int orderId)
    {
        _nextValidId = orderId;
        _logger.LogInformation("nextValidId={OrderId}", orderId);
    }

    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
    {
        try
        {
            if (!_scannerBuffers.TryGetValue(reqId, out var buffer)) 
            {
                _logger.LogWarning("scannerData: No buffer found for reqId={ReqId}", reqId);
                return;
            }

            var row = new ScannerRow
            {
                ReqId = reqId,
                Symbol = contractDetails.Contract.Symbol,
                Company = contractDetails.LongName ?? contractDetails.Contract.Symbol,
                LastPrice = 0,
                Change = 0,
                ChangePct = 0,
                Volume = 0,
                AvgVolume = 0,
                RelativeVolume = 0,
                Float = 0,
                High52W = 0,
                Meta = new InstrumentMetadata
                {
                    Symbol = contractDetails.Contract.Symbol,
                    Company = contractDetails.LongName ?? contractDetails.Contract.Symbol,
                    Sector = contractDetails.Industry ?? "Unknown",
                    Exchange = contractDetails.Contract.Exchange ?? "Unknown",
                    Region = "United States",
                    Product = "Stocks"
                }
            };
            
            buffer.Add(row);
            _logger.LogDebug("scannerData: reqId={ReqId}, rank={Rank}, symbol={Symbol}, buffer size={BufferSize}", 
                reqId, rank, row.Symbol, buffer.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scannerData callback");
        }
    }

    public void scannerDataEnd(int reqId)
    {
        try
        {
            if (_scannerWaiters.TryGetValue(reqId, out var tcs) && 
                _scannerBuffers.TryGetValue(reqId, out var buffer))
            {
                _logger.LogDebug("scannerDataEnd: reqId={ReqId}, rows={Count}", reqId, buffer.Count);
                
                tcs.TrySetResult(buffer);
            }
            else
            {
                _logger.LogWarning("scannerDataEnd: No waiter or buffer found for reqId={ReqId}", reqId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scannerDataEnd callback");
        }
    }

    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
        if (!_idToSymbol.TryGetValue(tickerId, out var symbol)) return;
        if (!_marketState.TryGetValue(symbol, out var state)) return;

        switch (field)
        {
            case TICK_LAST:
            case TICK_DELAYED_LAST:
                state.LastPrice = (decimal)price;
                break;
            case TICK_OPEN:
            case TICK_DELAYED_OPEN:
                state.Open = (decimal)price;
                break;
            case TICK_HIGH:
            case TICK_DELAYED_HIGH:
                state.High = (decimal)price;
                break;
            case TICK_LOW:
            case TICK_DELAYED_LOW:
                state.Low = (decimal)price;
                break;
            case TICK_CLOSE:
            case TICK_DELAYED_CLOSE:
                state.PrevClose = (decimal)price;
                break;
            default:
                return;
        }
        
        EmitTickUpdate(symbol, state);
    }

    public void tickSize(int tickerId, int field, decimal size)
    {
        if (field != TICK_VOLUME && field != TICK_DELAYED_VOLUME) return;
        
        if (!_idToSymbol.TryGetValue(tickerId, out var symbol)) return;
        if (!_marketState.TryGetValue(symbol, out var state)) return;
        
        state.Volume = (long)size;
        _logger.LogDebug("Volume update: {Symbol} = {Volume:N0}", symbol, (long)size);
        EmitTickUpdate(symbol, state);
    }

    public void historicalData(int reqId, Bar bar)
    {
        if (_histVolumes.TryGetValue(reqId, out var volumes) && bar.Volume > 0)
        {
            volumes.Add(bar.Volume);
        }
    }

    public void historicalDataEnd(int reqId, string startDate, string endDate)
    {
        if (_histReqToSymbol.TryGetValue(reqId, out var symbol) &&
            _histVolumes.TryGetValue(reqId, out var volumes))
        {
            if (volumes.Count > 0)
            {
                var avgVolume = (long)volumes.Average();
                _averageVolumes[symbol] = avgVolume;
                
                if (_marketState.TryGetValue(symbol, out var state))
                {
                    state.AverageVolume = avgVolume;
                    EmitTickUpdate(symbol, state);
                }
                
                _logger.LogDebug("Historical data: {Symbol} avgVolume={AvgVolume:N0}", symbol, avgVolume);
            }
            
            _histReqToSymbol.TryRemove(reqId, out _);
            _histVolumes.TryRemove(reqId, out _);
        }
    }

    private void EmitTickUpdate(string symbol, MarketState state)
    {
        try
        {
            var change = state.LastPrice.HasValue && state.PrevClose.HasValue
                ? state.LastPrice.Value - state.PrevClose.Value
                : 0m;
            
            var changePct = state.PrevClose.HasValue && state.PrevClose.Value > 0 && state.LastPrice.HasValue
                ? (state.LastPrice.Value - state.PrevClose.Value) / state.PrevClose.Value * 100
                : 0m;

            var relativeVolume = state.Volume.HasValue && state.AverageVolume.HasValue && state.AverageVolume.Value > 0
                ? (decimal)state.Volume.Value / state.AverageVolume.Value
                : 1m;

            var tickData = new TickData(
                Symbol: symbol,
                SessionId: Guid.Empty,
                LastPrice: (double?)state.LastPrice,
                ClosePrice: (double?)state.PrevClose,
                Volume: state.Volume,
                FiftyTwoWeekHigh: null,
                Timestamp: DateTime.UtcNow,
                Bid: null,
                Ask: null,
                High: (double?)state.High,
                Low: (double?)state.Low,
                Open: (double?)state.Open,
                PreviousClose: (double?)state.PrevClose,
                AverageVolume: state.AverageVolume,
                RelativeVolume: relativeVolume,
                Change: change,
                ChangePercent: changePct
            );

            _tickSubject.OnNext(tickData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting tick update for {Symbol}", symbol);
        }
    }

    #endregion

    #region Minimal EWrapper Implementation

    public void error(Exception e) => _logger.LogError(e, "IBKR Error");
    public void error(string str) => _logger.LogError("IBKR Error: {Message}", str);
    
    public void error(int id, int errorCode, string errorMsg)
    {
        // Log all errors appropriately
        if (errorCode == 492 || errorCode == 162)
        {
            _logger.LogWarning("IBKR Scanner Error {Code}: {Message}. This usually indicates region/exchange mismatch or missing subscriptions.", errorCode, errorMsg);
        }
        else if (errorCode == 165)
        {
            _logger.LogWarning("IBKR Error 165 (Session Conflict): {Message}. Scanner will return empty results. This is expected if another TWS/Gateway instance is running.", errorMsg);
        }
        else
        {
            _logger.LogError("IBKR Error {Code} for reqId {Id}: {Message}", errorCode, id, errorMsg);
        }

        // Handle scanner-specific errors
        if (_scannerWaiters.TryGetValue(id, out var tcs))
        {
            if (errorCode == 165 || errorCode == 162 || errorCode == 492)
            {
                // Complete with empty result for these errors
                tcs.TrySetResult(new List<ScannerRow>());
            }
        }
    }
    
    public void connectAck() => _logger.LogInformation("IBKR connection acknowledged");
    public void connectionClosed() => _logger.LogWarning("IBKR connection closed");
    public void currentTime(long time) { }
    public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice) { }
    public void tickGeneric(int tickerId, int field, double value) { }
    public void tickString(int tickerId, int field, string value) { }
    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate) { }
    public void tickSize(int tickerId, int field, int size)
    {
        // Delegate to decimal overload
        tickSize(tickerId, field, (decimal)size);
    }
    public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState) { }
    public void openOrderEnd() { }
    public void updateAccountValue(string key, string value, string currency, string accountName) { }
    public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName) { }
    public void updateAccountTime(string timestamp) { }
    public void accountDownloadEnd(string account) { }
    public void contractDetails(int reqId, ContractDetails contractDetails) { }
    public void bondContractDetails(int reqId, ContractDetails contractDetails) { }
    public void contractDetailsEnd(int reqId) { }
    public void execDetails(int reqId, Contract contract, Execution execution) { }
    public void execDetailsEnd(int reqId) { }
    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size) { }
    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth) { }
    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange) { }
    public void managedAccounts(string accountsList) { }
    public void receiveFA(int faDataType, string faXmlData) { }
    public void historicalDataUpdate(int reqId, Bar bar) { }
    public void scannerParameters(string xml){ }
    public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count) { }
    public void fundamentalData(int reqId, string data) { }
    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract) { }
    public void tickSnapshotEnd(int tickerId) { }
    public void marketDataType(int reqId, int marketDataType) { }
    public void commissionReport(CommissionReport commissionReport) { }
    public void position(string account, Contract contract, double pos, double avgCost) { }
    public void positionEnd() { }
    public void accountSummary(int reqId, string account, string tag, string value, string currency) { }
    public void accountSummaryEnd(int reqId) { }
    public void verifyMessageAPI(string apiData) { }
    public void verifyCompleted(bool isSuccessful, string errorText) { }
    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge) { }
    public void verifyAndAuthCompleted(bool isSuccessful, string errorText) { }
    public void displayGroupList(int reqId, string groups) { }
    public void displayGroupUpdated(int reqId, string contractInfo) { }
    public void positionMulti(int reqId, string account, string modelCode, Contract contract, double pos, double avgCost) { }
    public void positionMultiEnd(int reqId) { }
    public void accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency) { }
    public void accountUpdateMultiEnd(int reqId) { }
    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes) { }
    public void securityDefinitionOptionParameterEnd(int reqId) { }
    public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }
    public void familyCodes(FamilyCode[] familyCodes) { }
    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions) { }
    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions) { }
    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData) { }
    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap) { }
    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions) { }
    public void newsProviders(NewsProvider[] newsProviders) { }
    public void newsArticle(int requestId, int articleType, string articleText) { }
    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline) { }
    public void historicalNewsEnd(int requestId, bool hasMore) { }
    public void headTimestamp(int reqId, string headTimestamp) { }
    public void histogramData(int reqId, HistogramEntry[] data) { }
    public void rerouteMktDataReq(int reqId, int conId, string exchange) { }
    public void rerouteMktDepthReq(int reqId, int conId, string exchange) { }
    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements) { }
    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) { }
    public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value) { }
    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done) { }
    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done) { }
    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done) { }
    public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttribLast, string exchange, string specialConditions) { }
    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk) { }
    public void tickByTickMidPoint(int reqId, long time, double midPoint) { }
    public void orderBound(long orderId, int apiClientId, int apiOrderId) { }
    public void completedOrder(Contract contract, Order order, OrderState orderState) { }
    public void completedOrdersEnd() { }
    public void replaceFAEnd(int reqId, string text) { }
    public void wshMetaData(int reqId, string dataJson) { }
    public void wshEventData(int reqId, string dataJson) { }
    public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, object[] sessions) { }
    public void userInfo(int reqId, string whiteBrandingId) { }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_client?.IsConnected() == true)
            {
                _client.eDisconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from IBKR");
        }

        _tickSubject.OnCompleted();
        _tickSubject.Dispose();
    }
}

