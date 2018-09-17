using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class Instrument
    {
        [JsonProperty("name")]
        public string Name { get; }
    }
}
