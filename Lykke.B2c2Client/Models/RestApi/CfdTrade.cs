using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.RestApi
{
    public class CfdTrade : Trade
    {
        [JsonProperty("cfd_contract")]
        public string CfdContract { get; }
    }
}
