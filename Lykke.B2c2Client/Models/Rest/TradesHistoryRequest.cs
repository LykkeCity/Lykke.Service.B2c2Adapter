using System;

namespace Lykke.B2c2Client.Models.Rest
{
    public class TradesHistoryRequest : PaginationRequest
    {
        public DateTime? CreatedBefore { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public string Instrument { get; set; }
        public DateTime? Since { get; set; }
        public int Offset { get; set; }
    }
}
