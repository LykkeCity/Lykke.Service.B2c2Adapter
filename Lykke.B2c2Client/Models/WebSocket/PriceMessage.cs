using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        [JsonProperty("timestamp")]//, JsonConverter(typeof(UnixDateTimeConverter))] - doesn't work as unix date time
        public decimal Timestamp { get; set; }
    }
}
