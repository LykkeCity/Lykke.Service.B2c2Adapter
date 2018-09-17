using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class MarginRequirements
    {
        [JsonProperty("margin_requirement")]
        public decimal MarginRequirement { get; }

        [JsonProperty("currency")]
        public string Currency { get; }

        [JsonProperty("margin_usage")]
        public decimal MarginUsage { get; }

        [JsonProperty("equity")]
        public decimal Equity { get; }
    }
}
