using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class OrderRequest
    {
        /// A universally unique identifier that will be returned to you in the response.
        [JsonProperty("client_order_id")]
        public string ClientOrderId { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("order_type"), JsonConverter(typeof(StringEnumConverter))]
        public OrderType OrderType { get; set; }

        /// If true, B2C2 will open a new contract instead of closing the existing ones.
        [JsonProperty("force_open")]
        public bool ForceOpen { get; set; }

        [JsonProperty("valid_until")]
        public DateTime ValidUntil { get; set; }

        public OrderRequest()
        {
        }

        public OrderRequest(RequestForQuoteResponse requestForQuoteResponse)
        {
            ClientOrderId = requestForQuoteResponse.ClientRfqId;
            Instrument = requestForQuoteResponse.Instrument;
            Side = requestForQuoteResponse.Side;
            Price = requestForQuoteResponse.Price;
            Quantity = requestForQuoteResponse.Quantity;
            OrderType = OrderType.FOK;
            ValidUntil = requestForQuoteResponse.ValidUntil;
        }
    }
}
