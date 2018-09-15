using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.RestApi
{
    public class CfdTradeRequest
    {
        [JsonProperty("rfq_id")]
        public string Id { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        [JsonProperty("price")]
        public decimal Price { get; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; }

        /// If true, B2C2 will open a new contract instead of closing the existing ones.
        [JsonProperty("force_open")]
        public bool ForceOpen { get; }
    }
}
