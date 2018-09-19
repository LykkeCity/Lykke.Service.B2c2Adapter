using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class MarginRequirements
    {
        [JsonProperty("margin_requirement")]
        public decimal MarginRequirement { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("margin_usage")]
        public decimal MarginUsage { get; set; }

        [JsonProperty("equity")]
        public decimal Equity { get; set; }
    }
}
