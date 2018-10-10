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
using Lykke.Service.B2c2Adapter.RabbitPublishers;
using Lykke.Service.B2c2Adapter.Settings;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class OrderBooksService : IStartable, IStopable
    {
        private const string Source = "b2c2";
        private const string Suffix = ".SPOT";
        private readonly IReadOnlyCollection<InstrumentLevels> _instrumentsLevels;
        private readonly ConcurrentDictionary<string, string> _withWithoutSuffixMapping;
        private readonly ConcurrentDictionary<string, string> _withoutWithSuffixMapping;
        private readonly ConcurrentDictionary<string, OrderBook> _orderBooksCache;
        private readonly IB2С2RestClient _b2C2RestClient;
        private IB2С2WebSocketClient _b2C2WebSocketClient;
        private readonly B2C2ClientSettings _webSocketC2ClientSettings;
        private readonly IOrderBookPublisher _orderBookPublisher;
        private readonly ITickPricePublisher _tickPricePublisher;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private readonly object _syncReconnect = new object();
        private readonly TimeSpan _reconnectIfNeededInterval;
        private readonly TimerTrigger _reconnectIfNeededTrigger;
        private readonly TimerTrigger _forceReconnectTrigger;
        private readonly OrderBooksServiceSettings _settings;

        public OrderBooksService(
            IB2С2RestClient b2C2RestClient,
            IOrderBookPublisher orderBookPublisher,
            ITickPricePublisher tickPricePublisher,
            OrderBooksServiceSettings settings,
            B2C2ClientSettings webSocketC2ClientSettings,
            ILogFactory logFactory)
        {
            _withWithoutSuffixMapping = new ConcurrentDictionary<string, string>();
            _withoutWithSuffixMapping = new ConcurrentDictionary<string, string>();
            _orderBooksCache = new ConcurrentDictionary<string, OrderBook>();

            _instrumentsLevels = settings.InstrumentsLevels == null || !settings.InstrumentsLevels.Any() ? throw new ArgumentOutOfRangeException(nameof(_instrumentsLevels)) : settings.InstrumentsLevels;

            _b2C2RestClient = b2C2RestClient ?? throw new NullReferenceException(nameof(b2C2RestClient));
            _webSocketC2ClientSettings = webSocketC2ClientSettings ?? throw new NullReferenceException(nameof(webSocketC2ClientSettings));

            _orderBookPublisher = orderBookPublisher ?? throw new NullReferenceException(nameof(orderBookPublisher));
            _tickPricePublisher = tickPricePublisher ?? throw new NullReferenceException(nameof(tickPricePublisher));

            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);

            _reconnectIfNeededInterval = settings.ReconnectIfNeededInterval;
            _reconnectIfNeededTrigger = new TimerTrigger(nameof(OrderBooksService), settings.ReconnectIfNeededInterval, logFactory, ReconnectIfNeeded);
            _forceReconnectTrigger = new TimerTrigger(nameof(OrderBooksService), settings.ForceReconnectInterval, logFactory, ForceReconnect);

            _settings = settings;
        }

        public void Start()
        {
            InitializeAssetPairs();

            _reconnectIfNeededTrigger.Start();
            _forceReconnectTrigger.Start();

            ForceReconnect();
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
            return _settings;
        }

        private void InitializeAssetPairs()
        {
            IReadOnlyCollection<Instrument> instruments;

            _log.Info("Starting instrument initialization...");

            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    instruments = _b2C2RestClient.InstrumentsAsync().GetAwaiter().GetResult();
                    break;
                }
                catch (Exception e)
                {
                    _log.Info("Exception occured while getting instruments.", exception: e);
                }
            }

            foreach (var instrument in instruments)
            {
                var withoutSpotSuffix = InstrumentWoSuffix(instrument.Name);
                _withWithoutSuffixMapping[instrument.Name] = withoutSpotSuffix;
                _withoutWithSuffixMapping[withoutSpotSuffix] = instrument.Name;
            }

            _log.Info($"Finished instrument initialization, total instruments: {instruments.Count}.");
        }

        private void ForceReconnect()
        {
            _log.Info("Started subscribing.");

            var tasks = new List<Task>();

            lock (_syncReconnect)
            {
                // Try to subscribe until all instruments are subscribed
                while (true)
                {
                    _log.Info("Disposing WebSocketClient.");
                    _b2C2WebSocketClient?.Dispose();
                    _b2C2WebSocketClient = new B2С2WebSocketClient(_webSocketC2ClientSettings, _logFactory);

                    // Subscribing
                    foreach (var instrumentLevels in _instrumentsLevels)
                    {
                        var instrument = instrumentLevels.Instrument;
                        var instrumentWithSuffix = _withoutWithSuffixMapping[instrument];
                        var levels = instrumentLevels.Levels;

                        var task = _b2C2WebSocketClient.SubscribeAsync(instrumentWithSuffix, levels, HandleAsync)
                            .ContinueWith(x =>
                            {
                                if (x.Exception != null)
                                    _log.Info($"Exception while subscribing to {instrument}.", exception: x.Exception.InnerException);
                            });

                        tasks.Add(task);
                    }

                    var allSubscribed = Task.WaitAll(tasks.ToArray(), 10 * 1000);
                    if (allSubscribed)
                        break;
                }
            }

            _log.Info("Finished subscribing.");
        }

        private async Task HandleAsync(PriceMessage message)
        {
            var orderBook = Convert(message);
            var instrument = _withWithoutSuffixMapping[message.Instrument];
            if (_orderBooksCache.TryGetValue(instrument, out var existedOrderBook))
            {
                if (orderBook.Timestamp > existedOrderBook.Timestamp)
                {
                    _orderBooksCache[instrument] = orderBook;
                    await PublishOrderBookAndTickPrice(orderBook);
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

            var result = new OrderBook(Source, assetPair, priceMessage.Timestamp, asks, bids);

            return result;
        }

        private Task ReconnectIfNeeded(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            var isReceivedAtLeastOneOrderBookAtAll = _orderBooksCache.Values.Any();
            var haveAtLeastOneStaleOrderBookFromTheLastCheck =
                _orderBooksCache.Values.Any(x => DateTime.UtcNow - x.Timestamp > _reconnectIfNeededInterval);

            var needToReconnect = isReceivedAtLeastOneOrderBookAtAll &&
                                  haveAtLeastOneStaleOrderBookFromTheLastCheck;

            _log.Info($"Need to reconnect? {needToReconnect}.");

            if (needToReconnect)
                ForceReconnect();

            return Task.CompletedTask;
        }

        private Task ForceReconnect(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            _log.Info("Force reconnect by timer.");

            ForceReconnect();

            return Task.CompletedTask;
        }

        private async Task PublishOrderBookAndTickPrice(OrderBook orderBook)
        {
            await _orderBookPublisher.PublishAsync(orderBook);

            var tickPrice = TickPrice.FromOrderBook(orderBook);
            await _tickPricePublisher.PublishAsync(tickPrice);
        }

        private static IEnumerable<OrderBookItem> GetOrderBookItems(IEnumerable<QuantityPrice> quantitiesPrices)
        {
            var result = new List<OrderBookItem>();

            foreach (var qp in quantitiesPrices)
                result.Add(new OrderBookItem(qp.Price, qp.Quantity));

            return result;
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
