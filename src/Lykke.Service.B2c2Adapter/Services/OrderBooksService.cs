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
using Lykke.B2c2Client.Exceptions;
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
        private readonly ConcurrentDictionary<string, int> _healthCheck;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private readonly TimerTrigger _publishAllFromCacheTrigger;
        private readonly TimerTrigger _forceReconnectTrigger;

        public OrderBooksService(
            IReadOnlyList<InstrumentLevels> instrumentsLevels,
            IB2С2RestClient b2C2RestClient,
            IB2С2WebSocketClient b2C2WebSocketClient,
            IOrderBookPublisher orderBookPublisher,
            ITickPricePublisher tickPricePublisher,
            TimeSpan publishFromCacheInterval,
            TimeSpan forceReconnectInterval,
            B2C2ClientSettings webSocketC2ClientSettings,
            ILogFactory logFactory)
        {
            _withWithoutSuffixMapping = new ConcurrentDictionary<string, string>();
            _withoutWithSuffixMapping = new ConcurrentDictionary<string, string>();
            _orderBooksCache = new ConcurrentDictionary<string, OrderBook>();

            _instrumentsLevels = instrumentsLevels == null || !instrumentsLevels.Any() ? throw new ArgumentOutOfRangeException(nameof(_instrumentsLevels)) : instrumentsLevels;
            _b2C2RestClient = b2C2RestClient ?? throw new NullReferenceException(nameof(b2C2RestClient));
            _b2C2WebSocketClient = b2C2WebSocketClient ?? throw new NullReferenceException(nameof(b2C2RestClient));
            _webSocketC2ClientSettings = webSocketC2ClientSettings ?? throw new NullReferenceException(nameof(webSocketC2ClientSettings));
            _orderBookPublisher = orderBookPublisher ?? throw new NullReferenceException(nameof(orderBookPublisher));
            _tickPricePublisher = tickPricePublisher ?? throw new NullReferenceException(nameof(tickPricePublisher));
            _healthCheck = new ConcurrentDictionary<string, int>();
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
            _publishAllFromCacheTrigger = new TimerTrigger(nameof(OrderBooksService), publishFromCacheInterval, logFactory, PublishAllFromCache);
            _forceReconnectTrigger = new TimerTrigger(nameof(OrderBooksService), forceReconnectInterval, logFactory, ForceReconnect);
        }

        public void Start()
        {
            InitializeAssetPairs();
            SubscribeToOrderBooks();

            _publishAllFromCacheTrigger.Start();
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

        private void InitializeAssetPairs()
        {
            var instruments = _b2C2RestClient.InstrumentsAsync().GetAwaiter().GetResult();
            foreach (var instrument in instruments)
            {
                var withoutSpotSuffix = InstrumentWoSuffix(instrument.Name);
                _withWithoutSuffixMapping[instrument.Name] = withoutSpotSuffix;
                _withoutWithSuffixMapping[withoutSpotSuffix] = instrument.Name;
            }
        }

        private void SubscribeToOrderBooks()
        {
            var subscribed = 0;
            var skipped = 0;

            using (var enumerator = _instrumentsLevels.GetEnumerator())
            {
                enumerator.MoveNext();
                while (_instrumentsLevels.Count != subscribed + skipped)
                {
                    var instrumentLevels = enumerator.Current;
                    var instrument = instrumentLevels.Instrument;

                    if (_withWithoutSuffixMapping.ContainsKey(instrument))
                    {
                        _log.Warning($"Didn't find instrument {instrument}.");
                        skipped++;
                        continue;
                    }

                    var instrumentWithSuffix = _withoutWithSuffixMapping[instrument];
                    var levels = instrumentLevels.Levels;

                    try
                    {
                        _b2C2WebSocketClient.SubscribeAsync(instrumentWithSuffix, levels, HandleAsync).GetAwaiter().GetResult();
                        subscribed++;
                        enumerator.MoveNext();
                    }
                    catch (B2c2WebSocketException e)
                    {
                        _log.Warning($"Can't subscribe to instrument {instrumentWithSuffix}. {e.Message}");
                    }
                }
            }

            _log.Info($"Subscribed to {subscribed} of {_instrumentsLevels.Count}.");
        }

        private async Task HandleAsync(PriceMessage message)
        {
            var orderBook = Convert(message);
            var instrument = _withWithoutSuffixMapping[message.Instrument];
            _orderBooksCache[instrument] = orderBook;

            await PublishOrderBookAndTickPrice(orderBook);

            SetHelthCheck(message);
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

        private async Task PublishAllFromCache(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            try
            {
                await WriteHealthCheck();

                _log.Info("Publishing from cache...");

                foreach (var orderBook in _orderBooksCache.Values)
                    await PublishOrderBookAndTickPrice(orderBook);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            _log.Info("Published from cache.");
        }

        private Task ForceReconnect(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            try
            {
                _log.Info("Force reconnection...");

                _b2C2WebSocketClient.Dispose();
                _b2C2WebSocketClient = new B2С2WebSocketClient(_webSocketC2ClientSettings, _logFactory);
                SubscribeToOrderBooks();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            _log.Info("Finished reconnection.");

            return Task.CompletedTask;
        }

        private async Task PublishOrderBookAndTickPrice(OrderBook orderBook)
        {
            // Publish order books
            await _orderBookPublisher.PublishAsync(orderBook);

            // Publish tick prices
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

        private void SetHelthCheck(PriceMessage result)
        {
            if (_healthCheck.TryGetValue(result.Instrument, out var counter))
                _healthCheck[result.Instrument] = counter + 1;
            else
                _healthCheck[result.Instrument] = 1;
        }

        private Task WriteHealthCheck()
        {
            var list = _healthCheck.OrderBy(x => x.Value)
                                   .Select(x => x.Key)
                                   .Select(key => $"{key} : {_healthCheck[key]}")
                                   .ToList();
            try
            {
                _log.Info($"Health check: {Environment.NewLine}{string.Join(Environment.NewLine, list)}");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            foreach (var key in _healthCheck.Keys)
                _healthCheck[key] = 0;

            return Task.CompletedTask;
        }

        public void Stop()
        {
            _publishAllFromCacheTrigger.Stop();
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

            if (_publishAllFromCacheTrigger != null)
            {
                _publishAllFromCacheTrigger.Stop();
                _publishAllFromCacheTrigger.Dispose();
            }
        }

        #endregion
    }
}
