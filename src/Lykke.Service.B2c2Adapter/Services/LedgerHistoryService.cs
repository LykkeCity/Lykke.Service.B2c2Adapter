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
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.EntityFramework;
using Lykke.Service.B2c2Adapter.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class LedgerHistoryService: IStartable, IStopable
    {
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly string _sqlConnString;
        private readonly bool _enableAutoUpdate;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private TimerTrigger _timer;
        private readonly object _gate = new object();
        private bool _isActiveWork = false;

        public LedgerHistoryService(IB2С2RestClient b2C2RestClient, string sqlConnString, bool enableAutoUpdate, ILogFactory logFactory)
        {
            _b2C2RestClient = b2C2RestClient;
            _sqlConnString = sqlConnString;
            _enableAutoUpdate = enableAutoUpdate;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
        }

        public async Task<int> ReloadLedgerHistoryAsync()
        {
            while (!StartWork())
                await Task.Delay(1000);

            try
            {
                using (var context = CreateContext())
                {
                    var offset = 0;
                    var data = await _b2C2RestClient.GetLedgerHistoryAsync(offset, 100);
                    _log.Info($"Current offset={offset}; load more {data.Count}");

                    var query = $"TRUNCATE TABLE {Constants.Schema}.{Constants.LedgersTable}";
                    _log.Info($"TRUNCATE TABLE {Constants.Schema}.{Constants.LedgersTable}");

                    await context.Database.ExecuteSqlCommandAsync(query);

                    while (data.Any())
                    {
                        var items = data.Select(e => new LedgerEntity(e)).ToList();
                        context.Ledgers.AddRange(items);
                        await context.SaveChangesAsync();

                        offset += data.Count;

                        data = await GetDataFromB2C2(offset);

                        _log.Info($"Current offset={offset}; load more {data.Count}");
                    }
                    
                    return offset;
                }
            }
            finally
            {
                StopWork();
            }
        }

        private async Task<List<LedgerLog>> GetDataFromB2C2(int offset)
        {
            while (true)
            {
                try
                {
                    var data = await _b2C2RestClient.GetLedgerHistoryAsync(offset, 100);
                    return data;
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("Request was throttled"))
                    {
                        _log.Info($"Request was throttled, wait 60 second");
                        await Task.Delay(60000);
                    }
                    else throw;
                }
            }
        }

        private ReportContext CreateContext()
        {
            return new ReportContext(_sqlConnString);
        }

        private async Task DoTimer(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken ct)
        {
            if (!StartWork())
                return;

            try
            {
                using (var context = CreateContext())
                {
                    var offset = 0;
                    var data = await _b2C2RestClient.GetLedgerHistoryAsync(offset, 10, ct);

                    var added = 0;
                    do
                    {
                        added = 0;
                        foreach (var log in data)
                        {
                            var item = await context.Ledgers.FirstOrDefaultAsync(
                                e => e.TransactionId == log.TransactionId, ct);
                            if (item != null)
                                continue;

                            item = new LedgerEntity(log);
                            context.Ledgers.Add(item);
                            added++;
                        }

                        await context.SaveChangesAsync(ct);
                        offset += data.Count;
                        data = await _b2C2RestClient.GetLedgerHistoryAsync(offset, 10, ct);
                    } while (added > 0);
                }
            }
            finally 
            {
                StopWork();
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

        private bool StartWork()
        {
            lock (_gate)
            {
                if (!_isActiveWork)
                {
                    _isActiveWork = true;
                    return true;
                }

                return false;
            }
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        private void StopWork()
        {
            lock (_gate)
            {
                if (_isActiveWork)
                {
                    _isActiveWork = false;
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
