using System;
using Lykke.B2c2Client.Converters;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class PriceMessage
    {
        [JsonProperty("levels")]
        public Levels Levels { get; set; }

        [JsonProperty("instrument")]
        public string Instrument { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("timestamp"), JsonConverter(typeof(UnixDateTimeFromMillisecondsConverter))]
        public DateTime Timestamp { get; set; }
    }
}
