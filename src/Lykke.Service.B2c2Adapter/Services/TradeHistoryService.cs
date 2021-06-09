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
    public class TradeHistoryService: IStartable, IStopable
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

        public TradeHistoryService(
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

        public async Task<int> ReloadTradeHistoryAsync()
        {
            while (!StartWork())
                await Task.Delay(1000);

            try
            {
                using (var context = CreateContext())
                {
                    var tradeRequest = new TradesHistoryRequest {Limit = 100};
                    string tradeId = null;

                    if (_enableAutoUpdate)
                    {
                        var last = context.Trades.OrderByDescending(x => x.Created).FirstOrDefault();
                        if (last != null)
                        {
                            tradeRequest.CreatedAfter = last.Created.AddHours(-1);
                            tradeId = last.TradeId;
                        }
                    }

                    var data = await _b2C2RestClient.GetTradeHistoryAsync(tradeRequest);

                    _log.Debug($"Current cursor = null; get data after tradeId = {(tradeId ?? "null")}; load more {data.Data.Count}");

                    int totalCount = 0;
                    bool finish = false;

                    while (!finish || data.Data.Count > 0)
                    {
                        var items = new List<TradeEntity>();

                        foreach (var item in data.Data)
                        {
                            foreach (var assetMapping in _assetMappings)
                                item.AssetPair = item.AssetPair.Replace(assetMapping.Key, assetMapping.Value);

                            items.Add(new TradeEntity(item));
                        }

                        foreach (var item in items)
                        {
                            if (!string.IsNullOrEmpty(tradeId) && item.TradeId == tradeId)
                            {
                                finish = true;
                                break;
                            }

                            totalCount++;
                            context.Trades.Add(item);
                        }

                        await context.SaveChangesAsync();

                        if (finish)
                        {
                            _log.Debug($"Finish loading to tradeId = {tradeId}. Loaded {totalCount} records");
                            break;
                        }

                        tradeRequest.Cursor = data.Next;
                        data = await _b2C2RestClient.GetTradeHistoryAsync(tradeRequest);

                        if (string.IsNullOrEmpty(data.Next))
                        {
                            finish = true;
                        }

                        _log.Debug($"Current cursor = {tradeRequest.Cursor}; next cursor: {tradeRequest.Cursor}; load more {data.Data.Count}");
                    }

                    return totalCount;
                }
            }
            finally
            {
                StopWork();
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
                    var tradeRequest = new TradesHistoryRequest {Limit = 10};
                    var data = await _b2C2RestClient.GetTradeHistoryAsync(tradeRequest, ct);

                    var added = 0;
                    do
                    {
                        added = 0;
                        foreach (var log in data.Data)
                        {
                            foreach (var assetMapping in _assetMappings)
                                log.AssetPair = log.AssetPair.Replace(assetMapping.Key, assetMapping.Value);

                            var item = await context.Trades.FirstOrDefaultAsync(e => e.TradeId == log.TradeId, ct);
                            if (item != null)
                                continue;

                            item = new TradeEntity(log);
                            context.Trades.Add(item);
                            added++;
                        }

                        await context.SaveChangesAsync(ct);
                        tradeRequest.Cursor = data.Next;
                        data = await _b2C2RestClient.GetTradeHistoryAsync(tradeRequest, ct);
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
