using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.RestApi
{
    public class OpenPosition
    {
        [JsonProperty("instrument")]
        public string Instrument { get; }

        [JsonProperty("side")]
        public Side Side { get; }

        [JsonProperty("avg_entry_price")]
        public decimal AverageEntryPrice { get; }

        [JsonProperty("agg_position")]
        public decimal AggregatePosition { get; }
    }
}
