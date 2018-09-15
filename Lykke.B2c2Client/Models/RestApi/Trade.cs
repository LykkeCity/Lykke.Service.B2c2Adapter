using System;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.RestApi
{
    public class Trade
    {
        [JsonProperty("trade_id")]
        public string TradeId { get; }

        [JsonProperty("rfq_id")]
        public string RfqId { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        [JsonProperty("price")]
        public decimal Price { get; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; }

        [JsonProperty("order")]
        public string Order { get; }

        [JsonProperty("created")]
        public DateTime Created { get; }
    }
}
