using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class UnsubscribeMessage
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
