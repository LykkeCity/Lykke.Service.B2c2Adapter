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

        /// Instrument as given by the /instruments/ endpoint.
        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        /// Either ‘buy’ or ‘sell’.
        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        /// Price at which you want the order to be executed. Only 'FOK' order type.
        [JsonProperty("price", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Price { get; set; }

        /// Quantity in base currency (maximum 4 decimals).
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        /// Only ‘FOK’ and ‘MKT’ accepted for now.
        [JsonProperty("order_type"), JsonConverter(typeof(StringEnumConverter))]
        public OrderType OrderType { get; set; }

        /// If true, B2C2 will open a new contract instead of closing the existing ones.
        [JsonProperty("force_open", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ForceOpen { get; set; }

        /// Datetime field formatted “%Y-%m-%dT%H:%M:%S”.
        [JsonProperty("valid_until")]
        public DateTime ValidUntil { get; set; }

        /// Acceptable leeway in bps, between 0 and 20. Only FOK. (maximum 2 decimals)
        [JsonProperty("acceptable_slippage_in_basis_points")]
        public int AcceptableSlippage { get; set; }

        /// Optional. Tag that the customer can assign to an order to link it to client side logic. It’s not required and it can be duplicated.
        [JsonProperty("executing_unit", NullValueHandling = NullValueHandling.Ignore)]
        public string ExecutingUnit { get; set; }
    }
}
