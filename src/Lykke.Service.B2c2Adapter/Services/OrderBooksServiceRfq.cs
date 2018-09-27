using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.RabbitPublishers;
using Lykke.Service.B2c2Adapter.Settings;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class OrderBooksServiceRfq: IStartable, IStopable
    {
        private const string Source = "b2c2";
        private readonly TimeSpan _rfqRequestsSleepInterval;
        private readonly IReadOnlyList<InstrumentLevels> _instrumentsLevels;
        private readonly TimerTrigger _trigger;
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly IOrderBookPublisherRfq _orderBookPublisherRfq;
        private readonly ITickPricePublisherRfq _tickPricePublisherRfq;
        private readonly ILog _log;

        public OrderBooksServiceRfq(
            IReadOnlyList<InstrumentLevels> instrumentsLevels,
            TimeSpan sleepInterval,
            TimeSpan rfqRequestsSleepInterval,
            IB2С2RestClient b2C2RestClient,
            IOrderBookPublisherRfq orderBookPublisherRfq,
            ITickPricePublisherRfq tickPricePublisherRfq,
            ILogFactory logFactory)
        {
            _instrumentsLevels = instrumentsLevels == null || !instrumentsLevels.Any() ? throw new ArgumentOutOfRangeException(nameof(_instrumentsLevels)) : instrumentsLevels;
            _rfqRequestsSleepInterval = rfqRequestsSleepInterval;
            _trigger = new TimerTrigger(nameof(OrderBooksServiceRfq), sleepInterval, logFactory, Execute);
            _b2C2RestClient = b2C2RestClient ?? throw new NullReferenceException(nameof(b2C2RestClient));
            _orderBookPublisherRfq = orderBookPublisherRfq ?? throw new NullReferenceException(nameof(orderBookPublisherRfq));
            _tickPricePublisherRfq = tickPricePublisherRfq ?? throw new NullReferenceException(nameof(tickPricePublisherRfq));
            _log = logFactory.CreateLog(this);
        }

        private async Task Execute(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            try
            {
                await Execute();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private async Task Execute()
        {
            foreach (var instrumentLevels in _instrumentsLevels)
            {
                var instrument = instrumentLevels.Instrument;
                var levels = instrumentLevels.Levels;

                var bids = new List<OrderBookItem>();
                var asks = new List<OrderBookItem>();

                foreach (var level in levels)
                {
                    var request = new RequestForQuoteRequest(instrument, Side.Sell, level);
                    var bid = await _b2C2RestClient.RequestForQuoteAsync(request);
                    await Task.Delay(_rfqRequestsSleepInterval);
                    request.Side = Side.Buy;
                    var ask = await _b2C2RestClient.RequestForQuoteAsync(request);
                    await Task.Delay(_rfqRequestsSleepInterval);

                    bids.Add(new OrderBookItem(bid.Price, bid.Quantity));
                    asks.Add(new OrderBookItem(ask.Price, ask.Quantity));
                }

                var orderBook = new OrderBook(Source, instrument, DateTime.UtcNow, asks, bids);
                await _orderBookPublisherRfq.PublishAsync(orderBook);

                var tickPrice = TickPrice.FromOrderBook(orderBook);
                await _tickPricePublisherRfq.PublishAsync(tickPrice);
            }
        }

        #region IStartable, IStopable

        public void Start()
        {
            _trigger.Start();
        }

        public void Stop()
        {
            _trigger.Stop();
        }

        public void Dispose()
        {
            _trigger?.Dispose();
        }

        #endregion
    }
}
