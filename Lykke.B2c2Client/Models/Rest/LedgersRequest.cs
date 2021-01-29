using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.B2c2Client.Models.Rest
{
    public class LedgersRequest : PaginationRequest
    {
        public DateTime? CreatedBefore { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public string Currency { get; set; }
        public LedgerType? Type { get; set; }
        public DateTime? Since { get; set; }
        public int Offset { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LedgerType
    {
        [EnumMember(Value = "realised_pnl")]
        RealizedPnl,
        [EnumMember(Value = "funding")]
        Funding,
        [EnumMember(Value = "trade")]
        Trade,
        [EnumMember(Value = "transfer")]
        Transfer
    }
}
