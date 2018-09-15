using System;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.RestApi
{
    public class RequestForQuoteResponse
    {
        [JsonProperty("rfq_id")]
        public string Id { get; }

        /// A universally unique identifier that will be returned to you in the response.
        [JsonProperty("client_rfq_id")]
        public string ClientRfqId { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        [JsonProperty("price")]
        public decimal Price { get; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; }

        [JsonProperty("created")]
        public DateTime Created { get; }

        [JsonProperty("valid_until")]
        public DateTime ValidUntil { get; }
    }
}
