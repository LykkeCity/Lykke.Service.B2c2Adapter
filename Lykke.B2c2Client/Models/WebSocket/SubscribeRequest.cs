using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class SubscribeRequest : IRequest
    {
        [JsonProperty("event")]
        public string Event { get; set; } = "subscribe";

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("levels")]
        public decimal[] Levels { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }
    }
}
