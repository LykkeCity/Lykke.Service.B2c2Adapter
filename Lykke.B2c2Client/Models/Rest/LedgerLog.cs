using System;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class LedgerLog
    {
        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }
    }
}
