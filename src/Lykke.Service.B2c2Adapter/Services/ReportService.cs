using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Lykke.Common.Log;
using Microsoft.EntityFrameworkCore;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class ReportService: IStartable, IStopable
    {
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly string _sqlConnString;
        private readonly bool _enableAutoUpdate;
        private readonly ILogFactory _logFactory;
        private TimerTrigger _timer;
        private readonly object _gate = new object();
        private bool _isActiveWork = false;

        public ReportService(IB2С2RestClient b2C2RestClient, string sqlConnString, bool enableAutoUpdate, ILogFactory logFactory)
        {
            _b2C2RestClient = b2C2RestClient;
            _sqlConnString = sqlConnString;
            _enableAutoUpdate = enableAutoUpdate;
            _logFactory = logFactory;
        }

        public async Task<int> ReloadTradeHistoryAsync()
        {
            while (!StartWork())
                await Task.Delay(1000);

            try
            {
                using (var context = GetContext())
                {
                    var offset = 0;
                    var data = await _b2C2RestClient.GetTradeHistoryAsync(offset, 100);

                    await context.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE dbo.B2C2Trades");

                    while (data.Any())
                    {
                        var items = data.Select(e => new TradeEntity(e)).ToList();
                        context.Trades.AddRange(items);
                        await context.SaveChangesAsync();

                        offset += data.Count;
                        data = await _b2C2RestClient.GetTradeHistoryAsync(offset, 100);
                    }
                    
                    return offset;
                }
            }
            finally
            {
                StopWork();
            }
        }


        private ReportContext GetContext()
        {
            return new ReportContext(_sqlConnString);
        }

        public void Start()
        {
            _timer = new TimerTrigger(nameof(ReportService), TimeSpan.FromMinutes(1), _logFactory, DoTimer);
        }

        private async Task DoTimer(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationtoken)
        {
            if (!StartWork())
                return;

            try
            {
                using (var context = GetContext())
                {
                    var offset = 0;
                    var data = await _b2C2RestClient.GetTradeHistoryAsync(offset, 10, cancellationtoken);

                    var countNew = 0;
                    do
                    {
                        foreach (var log in data)
                        {
                            var item = await context.Trades.FirstOrDefaultAsync(e => e.TradeId == log.TradeId, cancellationtoken);
                            if (item == null)
                            {
                                item = new TradeEntity(log);
                                context.Trades.Add(item);
                                countNew++;
                            }
                        }

                        await context.SaveChangesAsync(cancellationtoken);
                        offset += data.Count;

                    } while (countNew > 0);
                }
            }
            finally 
            {
                StopWork();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void Stop()
        {
            _timer?.Stop();
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

        private bool StopWork()
        {
            lock (_gate)
            {
                if (_isActiveWork)
                {
                    _isActiveWork = false;
                    return true;
                }

                return false;
            }
        }
    }

    [Table("B2C2Trades", Schema = "dbo")]
    public class TradeEntity
    {
        public TradeEntity()
        {
        }

        public TradeEntity(TradeLog trade)
        {
            Update(trade);
        }

        [Key]
        public string TradeId { get; set; }

        public string RequestForQuoteId { get; set; }

        public decimal Volume { get; set; }

        public string Direction { get; set; }

        public string AssetPair { get; set; }

        public decimal Price { get; set; }

        public DateTime Created { get; set; }

        public void Update(TradeLog trade)
        {
            TradeId = trade.TradeId;
            RequestForQuoteId = trade.RequestForQuoteId ?? "";
            Volume = trade.Volume;
            Direction = trade.Direction;
            AssetPair = trade.AssetPair;
            Price = trade.Price;
            Created = trade.Created;
        }
    }

    public class ReportContext : DbContext
    {
        private readonly string _connectionString;

        public ReportContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<TradeEntity> Trades { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}
