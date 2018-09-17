using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class TradeRequest
    {
        [JsonProperty("rfq_id")]
        public string Id { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; }

        [JsonProperty("price")]
        public decimal Price { get; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; }
    }
}
