using System;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class OrderRequest
    {
        /// A universally unique identifier that will be returned to you in the response.
        [JsonProperty("client_order_id")]
        public string ClientOrderId { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        [JsonProperty("price")]
        public decimal Price { get; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; }

        [JsonProperty("order_type")]
        public OrderType OrderType { get; }

        /// If true, B2C2 will open a new contract instead of closing the existing ones.
        [JsonProperty("force_open")]
        public bool ForceOpen { get; }

        [JsonProperty("valid_until")]
        public DateTime ValidUntil { get; }
    }
}
