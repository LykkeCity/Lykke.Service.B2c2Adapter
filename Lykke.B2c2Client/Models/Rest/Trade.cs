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
        public double Price { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("order")]
        public string Order { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }
    }
}
