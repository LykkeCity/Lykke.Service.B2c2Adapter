using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Lykke.B2c2Client.Models.WebSocket;
using Lykke.B2c2Client.Settings;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.RabbitMq.Publishers;
using Lykke.Service.B2c2Adapter.Settings;
using Lykke.Service.B2c2Adapter.Utils;
using Lykke.Service.B2c2Adapter.ZeroMq;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class OrderBooksService : IStartable, IStopable
    {
        private const string LegacySource = "b2c2";
        private const string Suffix = ".SPOT";
        private readonly IReadOnlyCollection<InstrumentLevels> _instrumentsLevels;
        private readonly ConcurrentDictionary<string, string> _withWithoutSuffixMapping;
        private readonly ConcurrentDictionary<string, string> _withoutWithSuffixMapping;
        private readonly ConcurrentDictionary<string, OrderBook> _orderBooksCache;
        private readonly ConcurrentDictionary<string, string> _subscriptions;
        private readonly IB2С2RestClient _b2C2RestClient;
        private IB2С2WebSocketClient _b2C2WebSocketClient;
        private readonly B2C2ClientSettings _webSocketC2ClientSettings;
        private readonly ITickPricePublisher _tickPricePublisher;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private readonly object _syncReconnect = new();
        private readonly TimeSpan _reconnectIfNeededInterval;
        private readonly TimerTrigger _reconnectIfNeededTrigger;
        private readonly TimerTrigger _forceReconnectTrigger;
        private readonly B2c2AdapterSettings _settings;
        private readonly OrderBooksServiceSettings _orderBooksServiceSettings;
        private readonly IReadOnlyDictionary<string, string> _assetMappings;
        private readonly IOrderBookPublisher _orderBookPublisher;
        private readonly ZeroMqOrderBookPublisher _zeroMqOrderBookPublisher;

        public OrderBooksService(
            IB2С2RestClient b2C2RestClient,
            IOrderBookPublisher orderBookPublisher,
            ZeroMqOrderBookPublisher zeroMqOrderBookPublisher,
            ITickPricePublisher tickPricePublisher,
            B2c2AdapterSettings settings,
            B2C2ClientSettings webSocketC2ClientSettings,
            IReadOnlyDictionary<string, string> assetMappings,
            ILogFactory logFactory)
        {
            _withWithoutSuffixMapping = new ConcurrentDictionary<string, string>();
            _withoutWithSuffixMapping = new ConcurrentDictionary<string, string>();
            _orderBooksCache = new ConcurrentDictionary<string, OrderBook>();
            _subscriptions = new ConcurrentDictionary<string, string>();

            _settings = settings;

            var orderBooksServiceSettings = new OrderBooksServiceSettings
            {
                InstrumentsLevels = settings.InstrumentLevels,
                ReconnectIfNeededInterval = settings.ReconnectIfNeededInterval,
                ForceReconnectInterval = settings.ForceReconnectInterval
            };

            _orderBooksServiceSettings = orderBooksServiceSettings;

            _instrumentsLevels = _orderBooksServiceSettings.InstrumentsLevels == null || !_orderBooksServiceSettings.InstrumentsLevels.Any()
                ? throw new ArgumentOutOfRangeException(nameof(_instrumentsLevels)) : _orderBooksServiceSettings.InstrumentsLevels;

            _b2C2RestClient = b2C2RestClient ?? throw new NullReferenceException(nameof(b2C2RestClient));
            _webSocketC2ClientSettings = webSocketC2ClientSettings ?? throw new NullReferenceException(nameof(webSocketC2ClientSettings));

            _zeroMqOrderBookPublisher = zeroMqOrderBookPublisher ?? throw new NullReferenceException(nameof(zeroMqOrderBookPublisher));
            _orderBookPublisher = orderBookPublisher ?? throw new NullReferenceException(nameof(orderBookPublisher));
            
            _tickPricePublisher = tickPricePublisher ?? throw new NullReferenceException(nameof(tickPricePublisher));

            _reconnectIfNeededInterval = settings.ReconnectIfNeededInterval;
            _reconnectIfNeededTrigger = new TimerTrigger(nameof(OrderBooksService), settings.ReconnectIfNeededInterval, logFactory, ReconnectIfNeeded);
            _forceReconnectTrigger = new TimerTrigger(nameof(OrderBooksService), settings.ForceReconnectInterval, logFactory, ForceReconnect);

            _assetMappings = assetMappings;

            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
        }

        public void Start()
        {
            InitializeAssetPairs();

            _reconnectIfNeededTrigger.Start();
            _forceReconnectTrigger.Start();
        }

        public IReadOnlyCollection<string> GetAllInstruments()
        {
            return _withoutWithSuffixMapping.Keys.OrderBy(x => x).ToList();
        }

        public IReadOnlyCollection<TickPrice> GetAllTickPrices()
        {
            return _orderBooksCache.Values.Select(TickPrice.FromOrderBook).OrderBy(x => x.Asset).ToList();
        }

        public OrderBook GetOrderBook(string assetPair)
        {
            if (!_orderBooksCache.ContainsKey(assetPair))
                return null;

            return _orderBooksCache[assetPair];
        }

        public OrderBooksServiceSettings GetSettings()
        {
            return _orderBooksServiceSettings;
        }

        private void InitializeAssetPairs()
        {
            IReadOnlyCollection<Instrument> instruments;

            _log.Info("Started instrument initialization.");

            while (true)
            {
                try
                {
                    Thread.Sleep(1000);

                    instruments = _b2C2RestClient.InstrumentsAsync().GetAwaiter().GetResult();

                    break;
                }
                catch (Exception exception)
                {
                    _log.Info("Exception occured while getting instruments via REST.", exception);
                }
            }

            foreach (var instrument in instruments)
            {
                var withoutSpotSuffix = InstrumentWoSuffix(instrument.Name);
                _withWithoutSuffixMapping[instrument.Name] = withoutSpotSuffix;
                _withoutWithSuffixMapping[withoutSpotSuffix] = instrument.Name;
            }

            _log.Info("Finished instrument initialization.", new { instruments.Count });
        }

        private async Task HandleAsync(PriceMessage message)
        {
            var instrument = _withWithoutSuffixMapping[message.Instrument];

            var assetPair = _settings.InstrumentMappings.FirstOrDefault(x => x.Value == instrument).Key;

            if (string.IsNullOrWhiteSpace(assetPair))
            {
                _log.Warning("Asset pair not found. {instrument}", instrument);
            }
            else
            {
                InternalMetrics.OrderBookInCount
                    .WithLabels(assetPair)
                    .Inc();

                InternalMetrics.OrderBookInDelayMilliseconds
                    .WithLabels(assetPair)
                    .Set((DateTime.UtcNow - message.Timestamp).TotalMilliseconds);
            }

            _log.Debug("Received for the first time on B2C2 connector - {assetPair}, {timestamp}.", instrument, new DateTimeOffset(message.Timestamp).ToUnixTimeMilliseconds());

            var orderBook = Convert(message);
            
            if (_orderBooksCache.TryGetValue(instrument, out var existedOrderBook))
            {
                if (orderBook.Timestamp > existedOrderBook.Timestamp)
                {
                    _orderBooksCache[instrument] = orderBook;

                    await PublishOrderBookAndTickPrice(orderBook, assetPair);
                }
            }
            else
            {
                _orderBooksCache[instrument] = orderBook;
            }
        }

        private static string InstrumentWoSuffix(string instrument)
        {
            Debug.Assert(instrument.Contains(Suffix));

            return instrument.Replace(Suffix, "");
        }

        private OrderBook Convert(PriceMessage priceMessage)
        {
            var assetPair = _withWithoutSuffixMapping[priceMessage.Instrument];

            var bids = GetOrderBookItems(priceMessage.Levels.Sell);
            var asks = GetOrderBookItems(priceMessage.Levels.Buy);

            var result = new OrderBook(LegacySource, assetPair, priceMessage.Timestamp, asks, bids);

            return result;
        }

        private async Task ReconnectIfNeeded(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            try
            {
                ReconnectIfNeeded();
            }
            catch (Exception e)
            {
                _log.Info("Error during ReconnectIfNeeded.", exception: e);
            }
        }

        private void ReconnectIfNeeded()
        {
            var hasAny = _orderBooksCache.Values.Any();
            var hasStale = _orderBooksCache.Values.Any(IsStale);
            var allSubscribed = _subscriptions.Count == _instrumentsLevels.Count;

            var needToReconnect = (hasAny && hasStale) || !allSubscribed;

            if (needToReconnect)
            {
                var oldest = _orderBooksCache.Values.OrderBy(x => x.Timestamp).FirstOrDefault();
                var secondsPassed = (DateTime.UtcNow - oldest?.Timestamp)?.TotalSeconds;

                _log.Info("Need to reconnect.", new
                {
                    oldest?.Asset,
                    secondsPassed,
                    hasAny,
                    hasStale,
                    allSubscribed,
                    subscriptions =_subscriptions.Count,
                    instruments =_instrumentsLevels.Count
                });

                ForceReconnect();
            }
            else
            {
                _log.Info("No need to reconnect.");
            }
        }

        private async Task ForceReconnect(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            _log.Info("Force reconnect by timer.");

            try
            {
                ForceReconnect();
            }
            catch (Exception e)
            {
                _log.Info("Error during ForceReconnect.", exception: e);
            }
        }

        private void ForceReconnect()
        {
            _log.Info("Forcing reconnect...");

            lock (_syncReconnect)
            {
                NotifyAboutDisconnect();
                
                _log.Info("Disposing WebSocketClient.");
                _b2C2WebSocketClient?.Dispose();
                _b2C2WebSocketClient = new B2С2WebSocketClient(_webSocketC2ClientSettings, _logFactory);

                _log.Info("Started subscribing.");
                foreach (var instrumentLevels in _instrumentsLevels)
                {
                    var instrument = instrumentLevels.Instrument;
                    var instrumentWithSuffix = _withoutWithSuffixMapping[instrument];
                    var levels = instrumentLevels.Levels;

                    _b2C2WebSocketClient.SubscribeAsync(instrumentWithSuffix, levels, HandleAsync)
                        .ContinueWith(x =>
                        {
                            if (x.Exception != null)
                                _log.Info("Exception while subscribing to an instrument.", exception: x.Exception.InnerException, context: new { instrument });
                            else
                                _subscriptions[instrument] = instrument;
                        });
                }
            }

            _log.Info("Finished subscribing.");
        }

        private void NotifyAboutDisconnect()
        {
            var emptyOrderBook = new OrderBook("", string.Empty, DateTime.UtcNow, Array.Empty<OrderBookItem>(), Array.Empty<OrderBookItem>());
            _log.Warning("Reconnect detected. Sending empty order book to 0mq.");
            _zeroMqOrderBookPublisher.PublishAsync(emptyOrderBook, "/");
        }
        
        private async Task PublishOrderBookAndTickPrice(OrderBook orderBook, string rawAssetPair)
        {
            if (IsStale(orderBook))
            {
                _log.Info("Stale instrument.", new { orderBook.Asset });

                return;
            }

            foreach (var assetMapping in _assetMappings)
                orderBook.Asset = orderBook.Asset.Replace(assetMapping.Key, assetMapping.Value);

            await _orderBookPublisher.PublishAsync(orderBook);
            await _zeroMqOrderBookPublisher.PublishAsync(orderBook, rawAssetPair);

            InternalMetrics.OrderBookOutCount
                .WithLabels(orderBook.Asset)
                .Inc();

            InternalMetrics.OrderBookOutDelayMilliseconds
                .WithLabels(orderBook.Asset)
                .Set((DateTime.UtcNow - orderBook.Timestamp).TotalMilliseconds);

            _log.Debug("Sent from B2C2 connector to Mixer - {assetPair}, {timestamp}.", orderBook.Asset, new DateTimeOffset(orderBook.Timestamp).ToUnixTimeMilliseconds());

            var tickPrice = TickPrice.FromOrderBook(orderBook);

            InternalMetrics.QuoteOutCount
                .WithLabels(tickPrice.Asset)
                .Inc();

            InternalMetrics.QuoteOutSidePrice
                .WithLabels(tickPrice.Asset, "ask")
                .Set((double) tickPrice.Ask);

            InternalMetrics.QuoteOutSidePrice
                .WithLabels(tickPrice.Asset, "bid")
                .Set((double) tickPrice.Bid);

            await _tickPricePublisher.PublishAsync(tickPrice);
        }

        private static IEnumerable<OrderBookItem> GetOrderBookItems(IEnumerable<QuantityPrice> quantitiesPrices)
        {
            var result = new List<OrderBookItem>();

            foreach (var qp in quantitiesPrices)
                result.Add(new OrderBookItem(qp.Price, qp.Quantity));

            return result;
        }

        private bool IsStale(OrderBook orderBook)
        {
            return DateTime.UtcNow - orderBook.Timestamp > _reconnectIfNeededInterval;
        }

        public void Stop()
        {
            _reconnectIfNeededTrigger.Stop();
            _forceReconnectTrigger.Stop();
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~OrderBooksService()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_reconnectIfNeededTrigger != null)
            {
                _reconnectIfNeededTrigger.Stop();
                _reconnectIfNeededTrigger.Dispose();
            }

            if (_forceReconnectTrigger != null)
            {
                _forceReconnectTrigger.Stop();
                _forceReconnectTrigger.Dispose();
            }
        }

        #endregion
    }
}
