using System.Runtime.Serialization;

namespace Lykke.B2c2Client.Models.Rest
{
    public enum Side
    {
        None = 0,

        [EnumMember(Value = "buy")]
        Buy = 1,

        [EnumMember(Value = "sell")]
        Sell = 2
    }
}
