using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class UnsubscribeRequest
    {
        [JsonProperty("event")]
        public string Event { get; set; } = "unsubscribe";

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }
    }
}
