using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class Error
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("from_documentation")]
        public string Documentation => CodesMessages.ContainsKey(Code) ? CodesMessages[Code] : "";

        private static readonly IDictionary<int, string> CodesMessages = new Dictionary<int, string>
        {
            { 1000, "Generic – Unknown error." },
            { 1001, "Instrument not allowed – Instrument does not exist or you are not authorized to trade it." },
            { 1002, "The RFQ does not belong to you." },
            { 1003, "Different instrument – You tried to post a trade with a different instrument than the related RFQ." },
            { 1004, "Different side – You tried to post a trade with a different side than the related RFQ." },
            { 1005, "Different price – You tried to post a trade with a different price than the related RFQ." },
            { 1006, "Different quantity – You tried to post a trade with a different quantity than the related RFQ." },
            { 1007, "Quote is not valid – Quote may have expired." },
            { 1009, "Price not valid – The price is not valid anymore. This error can occur during big market moves." },
            { 1010, "Quantity too big – Max quantity per trade reached." },
            { 1011, "Not enough balance – Not enough balance." },
            { 1012, "Max risk exposure reached – Please see our FAQ for more information about the risk exposure." },
            { 1013, "Max credit exposure reached – Please see our FAQ for more information about the credit exposure." },
            { 1014, "No BTC address associated – You don’t have a BTC address associated to your account." },
            { 1015, "Too many decimals – We only allow four decimals in quantities." },
            { 1016, "Trading is disabled – May occur after a maintenance or under exceptional circumstances." },
            { 1017, "Illegal parameter – Wrong type or parameter." },
            { 1018, "Settlement is disabled at the moment." },
            { 1019, "Quantity is too small." },
            { 1020, "The field valid_until is malformed." },
            { 1021, "Your Order has expired." },
            { 1022, "Currency not allowed." },
            { 1023, "We only support “FOK” order_type at the moment." },
            { 1101, "Field required – Field required." },
            { 1500, "This contract is already closed." },
            { 1501, "The given quantity must be smaller or equal to the contract quantity." },
            { 1502, "You don’t have enough margin. Please add funds to your account or close some positions." },
            { 1503, "Contract updates are only for closing a contract." },
            { 1100, "Other error." }
        };
    }
}
