using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private TimerTrigger _timer;

        public BalanceHistoryService(IB2С2RestClient b2C2RestClient, string sqlConnString, bool enableAutoUpdate, ILogFactory logFactory)
        {
            _b2C2RestClient = b2C2RestClient;
            _sqlConnString = sqlConnString;
            _enableAutoUpdate = enableAutoUpdate;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
        }
        
        private ReportContext CreateContext()
        {
            return new ReportContext(_sqlConnString);
        }

        private async Task DoTimer(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            var balance = await GetBalance(ct);
            if (balance == null)
                return;

            try
            {
                using (var context = CreateContext())
                {
                    var ts = DateTime.UtcNow;
                    var items = balance.Select(e => new BalanceEntity
                    {
                        Asset = e.Key,
                        Timestamp = ts,
                        Balance = e.Value
                    }).ToList();

                    context.Balances.AddRange(items);
                    await context.SaveChangesAsync(ct);
                }
            }
            catch (Exception e)
            {
                _log.Warning($"Exception occured while saving balance to the database: {e}.");
            }
        }

        private async Task<IReadOnlyDictionary<string, decimal>> GetBalance(CancellationToken ct = default(CancellationToken))
        {
            try
            {
                var balance = await _b2C2RestClient.BalanceAsync(ct);

                return balance;
            }
            catch (Exception e)
            {
                _log.Warning($"Exception occured while getting balance from B2C2: {e}.");
            }

            return null;
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
