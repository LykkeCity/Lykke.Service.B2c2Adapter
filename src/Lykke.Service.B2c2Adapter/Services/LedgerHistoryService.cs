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
        private TimerTrigger _timer;
        private readonly object _gate = new object();
        private bool _isActiveWork = false;
        private readonly IReadOnlyDictionary<string, string> _assetMappings;

        private readonly ILogFactory _logFactory;
        private readonly ILog _log;

        public LedgerHistoryService(
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

        public async Task<int> ReloadLedgerHistoryAsync()
        {
            while (!StartWork())
                await Task.Delay(1000);

            try
            {
                using (var context = CreateContext())
                {
                    var ledgerRequest = new LedgersRequest {Limit = 100};
                    string transactionId = null;

                    if (_enableAutoUpdate)
                    {
                        var last = context.Ledgers.OrderByDescending(x => x.Created).FirstOrDefault();
                        if (last != null)
                        {
                            ledgerRequest.CreatedAfter = last.Created.AddHours(-1);
                            transactionId = last.TransactionId;
                        }
                    }

                    var data = await _b2C2RestClient.GetLedgerHistoryAsync(ledgerRequest);
                    _log.Debug($"Current cursor = null; get data after transactionId = {(transactionId ?? "null")}; load more {data.Data.Count}");

                    int totalCount = 0;
                    bool finish = false;

                    while (!finish || data.Data.Count > 0)
                    {
                        var items = new List<LedgerEntity>();

                        foreach (var item in data.Data)
                        {
                            if (_assetMappings.ContainsKey(item.Currency))
                            {
                                item.Currency = _assetMappings[item.Currency];
                            }

                            items.Add(new LedgerEntity(item));
                        }

                        foreach (var item in items)
                        {
                            if (!string.IsNullOrEmpty(transactionId) && item.TransactionId == transactionId)
                            {
                                finish = true;
                                break;
                            }

                            totalCount++;
                            context.Ledgers.Add(item);
                        }

                        await context.SaveChangesAsync();

                        if (finish)
                        {
                            _log.Debug($"Finish loading to transactionId = {transactionId}. Loaded {totalCount} records");
                            break;
                        }

                        ledgerRequest.Cursor = data.Next;
                        data = await GetDataFromB2C2(ledgerRequest);

                        if (string.IsNullOrEmpty(data.Next))
                        {
                            finish = true;
                        }

                        _log.Debug($"Current cursor = {ledgerRequest.Cursor}; next cursor: {ledgerRequest.Cursor}; load more {data.Data.Count}");
                    }

                    return totalCount;
                }
            }
            finally
            {
                StopWork();
            }
        }

        private async Task<PaginationResponse<List<LedgerLog>>> GetDataFromB2C2(LedgersRequest request)
        {
            while (true)
            {
                try
                {
                    var data = await _b2C2RestClient.GetLedgerHistoryAsync(request);

                    foreach (var item in data.Data)
                    {
                        if (_assetMappings.ContainsKey(item.Currency))
                        {
                            item.Currency = _assetMappings[item.Currency];
                        }
                    }

                    return data;
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("Request was throttled"))
                    {
                        _log.Debug($"Request was throttled, wait 60 second");
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
                    var ledgerRequest = new LedgersRequest {Limit = 10};
                    var data = await _b2C2RestClient.GetLedgerHistoryAsync(ledgerRequest, ct);

                    var added = 0;
                    do
                    {
                        added = 0;
                        foreach (var log in data.Data)
                        {
                            if (_assetMappings.ContainsKey(log.Currency))
                            {
                                log.Currency = _assetMappings[log.Currency];
                            }

                            var item = await context.Ledgers.FirstOrDefaultAsync(
                                e => e.TransactionId == log.TransactionId, ct);

                            if (item != null)
                                continue;

                            item = new LedgerEntity(log);
                            context.Ledgers.Add(item);
                            added++;
                        }

                        await context.SaveChangesAsync(ct);
                        ledgerRequest.Cursor = data.Next;

                        data = await _b2C2RestClient.GetLedgerHistoryAsync(ledgerRequest, ct);
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
