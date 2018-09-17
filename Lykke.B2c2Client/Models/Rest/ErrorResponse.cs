using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.Rest
{
    public class ErrorResponse
    {
        public HttpStatusCode Status { get; set; }

        [JsonProperty("errors")]
        public IReadOnlyList<Error> Errors { get; set; } = new List<Error>();

        [JsonProperty("from_documentation")]
        public string Documentation => _codesMessages.ContainsKey((int)Status) ? _codesMessages[(int)Status] : "";

        private static IDictionary<int, string> _codesMessages = new Dictionary<int, string>
        {
            { 400, "Bad Request – Incorrect parameters." },
            { 401, "Unauthorized – Wrong Token." },
            { 404, "Not Found – The specified endpoint could not be found." },
            { 405, "Method Not Allowed – You tried to access an endpoint with an invalid method." },
            { 406, "Not Acceptable – Incorrect request format." },
            { 500, "Internal Server Error – We had a problem with our server. Try again later." }
        };
    }
}
