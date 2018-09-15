using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.RestApi
{
    public class OrderResponse
    {
        [JsonProperty("order_id")]
        public string OrderId { get; }

        /// A universally unique identifier that will be returned to you in the response.
        [JsonProperty("client_order_id")]
        public string ClientOrderId { get; }

        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        [JsonProperty("price")]
        public decimal Price { get; }

        /// The field executed_price, in the response, will contain the price at which the trade(s) has(ve) been executed,
        /// or null if the order was rejected.
        [JsonProperty("executed_price")]
        public decimal ExecutedPrice { get; }

        /// Quantity in base currency (maximum 4 decimals).
        /// The sum of the trades quantity should always be equal to the quantity of the order.
        [JsonProperty("quantity")]
        public decimal Quantity { get; }

        /// For SPOT trading, the list will always contain one element. For CFD trading, it may contain more.
        [JsonProperty("trades")]
        public IReadOnlyCollection<Trade> Trades { get; }

        [JsonProperty("created")]
        public DateTime Created { get; }
    }
}
