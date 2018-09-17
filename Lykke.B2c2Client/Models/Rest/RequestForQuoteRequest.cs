using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class RequestForQuoteRequest
    {
        /// A universally unique identifier that will be returned to you if the request succeeds.
        [JsonProperty("client_rfq_id")]
        public string ClientRfqId { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; }
    }
}
