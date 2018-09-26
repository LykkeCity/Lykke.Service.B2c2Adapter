using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class RequestForQuoteRequest
    {
        /// A universally unique identifier that will be returned to you if the request succeeds.
        [JsonProperty("client_rfq_id")]
        public string ClientRfqId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        public RequestForQuoteRequest()
        {
        }

        public RequestForQuoteRequest(string instrument, Side side, decimal quantity)
        {
            Instrument = instrument;
            Side = side;
            Quantity = quantity;
        }
    }
}
