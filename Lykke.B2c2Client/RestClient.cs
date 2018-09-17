using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.B2c2Client.Models.Rest;
using Lykke.B2c2Client.Services;
using Newtonsoft.Json.Linq;

namespace Lykke.B2c2Client
{
    public class RestClient : IRestService
    {
        private readonly string _authorizationToken;

        private static readonly HttpClient Client = new HttpClient
        {
            BaseAddress = new Uri("https://sandboxapi.b2c2.net/")
        };

        public RestClient(string authorizationToken)
        {
            _authorizationToken = authorizationToken;
            Client.DefaultRequestHeaders.Add("Authorization", $"Token {_authorizationToken}");
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetBalance(CancellationToken ct = default(CancellationToken))
        {
            using (var response = await Client.PostAsJsonAsync("balance/", new object(), ct))
            {
                var obj = await response.Content.ReadAsAsync<JObject>(ct);

                //EnsureNoErrorProperty(obj);

                return new Dictionary<string, decimal>();
                //return new GetWalletsResponse
                //{
                //    Wallets = ParseAmounts(obj).ToArray()
                //};
            }
        }

        public Task<IReadOnlyCollection<Instrument>> GetInstruments(CancellationToken ct = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<RequestForQuoteResponse> RequestForQuote(RequestForQuoteRequest requestForQuoteRequest, CancellationToken ct = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<OrderResponse> PostOrder(OrderRequest orderRequest, CancellationToken ct = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
        

        //public Task<GetWalletsResponse> GetBalance(CancellationToken ct = default(CancellationToken))
        //{
        //    return EpochNonce.Lock(_credentials.ApiKey, async nonce =>
        //    {
        //        var cmd = EmptyRequest(nonce);

        //        using (var response = await Client.PostAsJsonAsync("balance/", cmd, ct))
        //        {
        //            var obj = await response.Content.ReadAsAsync<JObject>(ct);

        //            EnsureNoErrorProperty(obj);

        //            return new GetWalletsResponse
        //            {
        //                Wallets = ParseAmounts(obj).ToArray()
        //            };
        //        }
        //    });
        //}
    }
}
