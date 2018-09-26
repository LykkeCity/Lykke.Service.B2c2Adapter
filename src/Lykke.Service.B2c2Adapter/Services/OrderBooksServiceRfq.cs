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
using Lykke.B2c2Client.Models.WebSocket;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.Settings;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class OrderBooksServiceRfq: IStartable, IStopable
    {
        private readonly IReadOnlyList<InstrumentLevels> _instrumentsLevels;
        private readonly TimerTrigger _trigger;
        private readonly Levels levels;
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly ILog _log;

        public OrderBooksServiceRfq(IReadOnlyList<InstrumentLevels> instrumentsLevels, TimeSpan sleepInterval,
            IB2С2RestClient b2C2RestClient, ILogFactory logFactory)
        {
            _instrumentsLevels = instrumentsLevels == null || !instrumentsLevels.Any() ? throw new ArgumentOutOfRangeException(nameof(_instrumentsLevels)) : instrumentsLevels.Where(x => x.Instrument == "BTCUSD").ToList().AsReadOnly();
            _trigger = new TimerTrigger(nameof(OrderBooksServiceRfq), sleepInterval, logFactory, Execute);
            _b2C2RestClient = b2C2RestClient ?? throw new NullReferenceException(nameof(b2C2RestClient));
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

                for (var i = 0; i < levels.Length; i++)
                {
                    var level = levels[i];
                    var request = new RequestForQuoteRequest(instrument, Side.Sell, level);
                    var bid = await _b2C2RestClient.RequestForQuoteAsync(request);
                    request.Side = Side.Buy;
                    var ask = await _b2C2RestClient.RequestForQuoteAsync(request);
                    if (i == 0)
                    {
                        bids.Add(new OrderBookItem(bid.Price, bid.Quantity));
                        asks.Add(new OrderBookItem(ask.Price, ask.Quantity));
                    }
                    else if (i == 1)
                    {
                        var prevBid = bids.SingleOrDefault();
                        var prevAsk = asks.SingleOrDefault();

                        var resultAskVolume = ask.Quantity - prevAsk.Volume;
                        var resultAskPrice = (ask.Price * ask.Quantity - prevAsk.Price * prevAsk.Volume) / resultAskVolume;

                    }
                }
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
