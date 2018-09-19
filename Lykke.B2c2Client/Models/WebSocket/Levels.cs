using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class Levels
    {
        [JsonProperty("buy")]
        public IReadOnlyList<QuantityPrice> Buy { get; set; } = new List<QuantityPrice>();

        [JsonProperty("sell")]
        public IReadOnlyList<QuantityPrice> Sell { get; set; } = new List<QuantityPrice>();
    }
}
