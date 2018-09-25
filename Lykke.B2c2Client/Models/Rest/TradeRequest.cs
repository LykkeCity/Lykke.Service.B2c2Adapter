using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class TradeRequest
    {
        [JsonProperty("rfq_id")]
        public string Id { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        public TradeRequest()
        {
        }

        public TradeRequest(RequestForQuoteResponse requestForQuoteResponse)
        {
            Id = requestForQuoteResponse.Id;
            Instrument = requestForQuoteResponse.Instrument;
            Side = requestForQuoteResponse.Side;
            Price = requestForQuoteResponse.Price;
            Quantity = requestForQuoteResponse.Quantity;
        }
    }
}
