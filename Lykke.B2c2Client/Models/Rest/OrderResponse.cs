using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class OrderResponse
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        /// A universally unique identifier that will be returned to you in the response.
        [JsonProperty("client_order_id")]
        public string ClientOrderId { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        /// The field executed_price, in the response, will contain the price at which the trade(s) has(ve) been executed,
        /// or null if the order was rejected.
        [JsonProperty("executed_price")]
        public decimal ExecutedPrice { get; set; }

        /// Quantity in base currency (maximum 4 decimals).
        /// The sum of the trades quantity should always be equal to the quantity of the order.
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        /// For SPOT trading, the list will always contain one element. For CFD trading, it may contain more.
        [JsonProperty("trades")]
        public IReadOnlyCollection<Trade> Trades { get; set; }

        [JsonProperty("created"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime Created { get; set; }
    }
}
