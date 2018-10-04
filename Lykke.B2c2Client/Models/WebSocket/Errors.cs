using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class Errors
    {
        [JsonProperty("instrument")]
        public string[] Instrument { get; set; }

        [JsonProperty("levels")]
        public string[] Levels { get; set; }
    }
}
