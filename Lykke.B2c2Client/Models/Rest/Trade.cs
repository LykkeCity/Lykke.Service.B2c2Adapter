using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class Trade
    {
        [JsonProperty("trade_id")]
        public string TradeId { get; set; }

        [JsonProperty("rfq_id")]
        public string RfqId { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("order")]
        public string Order { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        public Trade()
        {
        }

        public Trade(string tradeId, string rfqId, string instrument, Side side, double price, double quantity, string order, DateTime created)
        {
            TradeId = tradeId;
            RfqId = rfqId;
            Instrument = instrument;
            Side = side;
            Price = (decimal)price;
            Quantity = (decimal)quantity;
            Order = order;
            Created = created;
        }
    }
}
