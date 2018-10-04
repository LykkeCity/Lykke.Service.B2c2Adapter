using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class ErrorResponse
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("error_code")]
        public ErrorCode Code { get; set; }

        [JsonProperty("error_message")]
        public string Message { get; set; }

        [JsonProperty("errors")]
        public Errors Errors { get; set; }

        [JsonProperty("from_documentation")]
        public string Documentation => CodesMessages.ContainsKey((int)Code) ? CodesMessages[(int)Code] : "";

        private static readonly IDictionary<int, string> CodesMessages = new Dictionary<int, string>
        {
            { 3000, "Authentication failure." },
            { 3001, "Authorization not in the headers" },
            { 3002, "Endpoint does not exist – You are trying to access an endpoint that we don’t handle." },
            { 3003, "Instrument is not allowed – The instrument you are trying to subscribe to is not in your tradable instruments." },
            { 3004, "Subscription is invalid – The parameters for subscription are invalid. For details see Subscribing." },
            { 3005, "Unable to Jsonise your message – The message you sent is not valid json." },
            { 3006, "Already connected." },
            { 3007, "Already subscribed – You are already subscribed to this instrument." },
            { 3008, "Not subscribed yet – You are trying to unsubscribe to an instrument you are not subscribed." },
            { 3009, "InvalidFormat – Message is not string." },
            { 3011, "Invalid Message – There is something wrong with the message you sent (no event field…)." },
            { 3013, "Connectivity issues – Usually triggered when no price has been received for a given instrument during more than two seconds. Usually resolved quickly." },
            { 3014, "Unexpected error – We’re having connectivity issues. Please try later." },
            { 3015, "Username changed – Your username changed on the platform, this will close the connection." },
            { 3016, "Connectivity issues – Usually triggered by an issue with the pricer. Usually resolved quickly." },
            { 3017, "Authorization header is malformed. It must be of the form Token te25a9eb2588cf022d84e02cdf08c7405fadd164." },
            { 3018, "The max quantity for the instrument is been updated and the subscription for a level is not valid anymore. The connection will be closed." },
            { 3019, "The given instrument doesn’t end with .SPOT or .CFD." },
            { 4000, "Generic error – Unexpected error. Please reach out to the tech team." }
        };
    }
}
