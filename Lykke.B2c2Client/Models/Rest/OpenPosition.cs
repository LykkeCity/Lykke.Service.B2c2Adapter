using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class OpenPosition
    {
        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side"), JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; }

        [JsonProperty("avg_entry_price")]
        public decimal AverageEntryPrice { get; }

        [JsonProperty("agg_position")]
        public decimal AggregatePosition { get; }
    }
}
