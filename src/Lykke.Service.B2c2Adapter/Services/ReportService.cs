using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.Service.B2c2Adapter.Services
{
    public class ReportService
    {
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly string _sqlConnString;

        public ReportService(IB2С2RestClient b2C2RestClient, string sqlConnString)
        {
            _b2C2RestClient = b2C2RestClient;
            _sqlConnString = sqlConnString;
        }

        public async Task<int> ReloadTradeHistoryAsync()
        {
            using (var context = GetContext())
            {
                await context.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE dbo.B2C2Trades");

                var offset = 0;
                var data = await _b2C2RestClient.GetTradeHistoryAsync(offset, 100);

                while (data.Any())
                {
                    foreach (var log in data)
                    {
                        var trade = new TradeEntity(log);
                        context.Trades.Remove(trade);
                        context.Trades.Add(trade);
                    }

                    await context.SaveChangesAsync();

                    offset += data.Count;
                    data = await _b2C2RestClient.GetTradeHistoryAsync(offset, 100);
                }

                return offset;
            }
        }


        private ReportContext GetContext()
        {
            return new ReportContext(_sqlConnString);
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
            TradeId = trade.TradeId;
            RequestForQuoteId = trade.RequestForQuoteId;
            Volume = trade.Volume;
            Direction = trade.Direction;
            AssetPair = trade.AssetPair;
            Price = trade.Price;
            Created = trade.Created;
        }

        [Key]
        public string TradeId { get; set; }

        public string RequestForQuoteId { get; set; }

        public decimal Volume { get; set; }

        public string Direction { get; set; }

        public string AssetPair { get; set; }

        public decimal Price { get; set; }

        public DateTime Created { get; set; }
    }

    public class ReportContext : DbContext
    {
        public ReportContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        public DbSet<TradeEntity> Trades { get; set; }
    }
}
