using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class RequestForQuoteResponse
    {
        [JsonProperty("rfq_id")]
        public string Id { get; set; }

        /// A universally unique identifier that will be returned to you in the response.
        [JsonProperty("client_rfq_id")]
        public string ClientRfqId { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        // Absent
        //[JsonProperty("created"), JsonConverter(typeof(IsoDateTimeConverter))]
        //public DateTime Created { get; set; }

        [JsonProperty("valid_until"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime ValidUntil { get; set; }
    }
}
