using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.B2c2Client;
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.EntityFramework;
using Lykke.Service.B2c2Adapter.EntityFramework.Models;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class BalanceHistoryService: IStartable, IStopable
    {
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly string _sqlConnString;
        private readonly bool _enableAutoUpdate;
        private TimerTrigger _timer;
        private readonly IReadOnlyDictionary<string, string> _assetMappings;

        private readonly ILogFactory _logFactory;
        private readonly ILog _log;

        public BalanceHistoryService(
            IB2С2RestClient b2C2RestClient,
            string sqlConnString,
            bool enableAutoUpdate,
            IReadOnlyDictionary<string, string> assetMappings,
            ILogFactory logFactory)
        {
            _b2C2RestClient = b2C2RestClient;
            _sqlConnString = sqlConnString;
            _enableAutoUpdate = enableAutoUpdate;
            _assetMappings = assetMappings;

            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
        }

        private ReportContext CreateContext()
        {
            return new ReportContext(_sqlConnString);
        }

        private async Task DoTimer(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            var balance = await _b2C2RestClient.BalanceAsync(ct);

            using (var context = CreateContext())
            {
                var ts = DateTime.UtcNow;

                var items = new List<BalanceEntity>();

                foreach (var assetBalance in balance)
                {
                    var assetName = assetBalance.Key;

                    if (_assetMappings.ContainsKey(assetName))
                    {
                        assetName = _assetMappings[assetName];
                    }

                    var item = new BalanceEntity
                    {
                        Asset = assetName,
                        Timestamp = ts,
                        Balance = assetBalance.Value
                    };

                    items.Add(item);
                }

                context.Balances.AddRange(items);
                await context.SaveChangesAsync(ct);
            }
        }

        public void Start()
        {
            if (_enableAutoUpdate)
            {
                _timer = new TimerTrigger(nameof(TradeHistoryService), TimeSpan.FromMinutes(1), _logFactory, DoTimer);
                _timer.Start();
            }
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
