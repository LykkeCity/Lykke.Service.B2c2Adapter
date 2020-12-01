using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.Service.B2c2Adapter.EntityFramework.Models
{
    [Table(Constants.TradesTable, Schema = Constants.Schema)]
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

        public string User { get; set; }

        public DateTime Created { get; set; }

        public void Update(TradeLog trade)
        {
            TradeId = trade.TradeId;
            RequestForQuoteId = trade.RequestForQuoteId ?? "";
            Volume = trade.Volume;
            Direction = trade.Direction;
            AssetPair = trade.AssetPair;
            Price = trade.Price;
            User = trade.User;
            Created = trade.Created;
        }
    }
}
