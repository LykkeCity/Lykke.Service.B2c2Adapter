using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.B2c2Client.Exceptions;
using Lykke.B2c2Client.Models.Rest;
using Lykke.Common.Log;
using Newtonsoft.Json;

namespace Lykke.B2c2Client
{
    public class B2c2RestClient : IB2c2RestClient
    {
        private readonly string _authorizationToken;
        private readonly ILog _log;

        private readonly HttpClient _client;

        public B2c2RestClient(string url, string authorizationToken, ILogFactory logFactory)
        {
            url = url[url.Length - 1] == '/' ? url.Substring(0, url.Length - 1) : url;
            _client = new HttpClient { BaseAddress = new Uri(url) };
            _authorizationToken = authorizationToken;
            _client.DefaultRequestHeaders.Add("Authorization", $"Token {_authorizationToken}");
            _log = logFactory.CreateLog(this);
        }

        public async Task<IReadOnlyDictionary<string, decimal>> BalanceAsync(CancellationToken ct = default(CancellationToken))
        {
            var requestId = Guid.NewGuid();
            _log.Info("balance - request", requestId);

            try
            {
                using (var response = await _client.GetAsync("/balance", ct))
                {
                    var status = response.StatusCode;
                    
                    var responseStr = await response.Content.ReadAsStringAsync();
                    _log.Info($"balance - response: {responseStr}", requestId);

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Info($"balance - response exception: {e}", requestId);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Instrument>> InstrumentsAsync(CancellationToken ct = default(CancellationToken))
        {
            var requestId = Guid.NewGuid();
            _log.Info("instruments - request", requestId);

            try
            {
                using (var response = await _client.GetAsync("/instruments", ct))
                {
                    var status = response.StatusCode;

                    var responseStr = await response.Content.ReadAsStringAsync();
                    _log.Info($"instruments - response: {responseStr}", requestId);

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<IReadOnlyCollection<Instrument>>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Info($"instruments - response exception: {e}", requestId);
                throw;
            }
        }

        public async Task<RequestForQuoteResponse> RequestForQuoteAsync(RequestForQuoteRequest requestForQuoteRequest, CancellationToken ct = default(CancellationToken))
        {
            if (requestForQuoteRequest == null) throw new ArgumentNullException(nameof(requestForQuoteRequest));

            var requestId = Guid.NewGuid();
            _log.Info($"request_for_quote - request: {JsonConvert.SerializeObject(requestForQuoteRequest)}", requestId);

            try
            {
;               using (var response = await _client.PostAsJsonAsync("/request_for_quote", requestForQuoteRequest, ct))
                {
                    var status = response.StatusCode;

                    var responseStr = await response.Content.ReadAsStringAsync();
                    _log.Info($"request_for_quote - response: {responseStr}", requestId);

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<RequestForQuoteResponse>(responseStr);

                    if (result.ClientRfqId != requestForQuoteRequest.ClientRfqId)
                        throw new B2c2RestException($"request.client_rfq_id '{requestForQuoteRequest.ClientRfqId}' != " +
                                                    $"response.client_rfq_id '{result.ClientRfqId}'", requestId);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Info($"request_for_quote - response exception: {e}", requestId);
                throw;
            }
        }

        public async Task<OrderResponse> OrderAsync(OrderRequest orderRequest, CancellationToken ct = default(CancellationToken))
        {
            if (orderRequest == null) throw new ArgumentNullException(nameof(orderRequest));

            var requestId = Guid.NewGuid();
            _log.Info($"order - request: {JsonConvert.SerializeObject(orderRequest)}", requestId);

            try
            {
                using (var response = await _client.PostAsJsonAsync("/order", orderRequest, ct))
                {
                    var status = response.StatusCode;

                    var responseStr = await response.Content.ReadAsStringAsync();
                    _log.Info($"order - response: {responseStr}", requestId);

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<OrderResponse>(responseStr);

                    if (result.ClientOrderId != orderRequest.ClientOrderId)
                        throw new B2c2RestException($"request.client_order_id '{orderRequest.ClientOrderId}' != " +
                                                    $"response.client_order_id '{result.ClientOrderId}'", requestId);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Info($"order - response exception: {e}", requestId);
                throw;
            }
        }

        public async Task<Trade> TradeAsync(TradeRequest tradeRequest, CancellationToken ct = default(CancellationToken))
        {
            if (tradeRequest == null) throw new ArgumentNullException(nameof(tradeRequest));

            var requestId = Guid.NewGuid();
            _log.Info($"trade - request: {JsonConvert.SerializeObject(tradeRequest)}", requestId);

            try
            {
                using (var response = await _client.PostAsJsonAsync("/trade", tradeRequest, ct))
                {
                    var status = response.StatusCode;

                    var responseStr = await response.Content.ReadAsStringAsync();
                    _log.Info($"trade - response: {responseStr}", requestId);

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<Trade>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Info($"trade - response exception: {e}", requestId);
                throw;
            }
        }


        private void CheckForError(string response, HttpStatusCode status, Guid guid)
        {
            if (response.Contains("errors"))
            {
                ErrorResponse errorResponse;
                try
                {
                    errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response);
                    errorResponse.Status = status;
                }
                catch (Exception e)
                {
                    var message = $"Can't deserialize error response, status: {(int)status} {status.ToString()}, guid: {guid}, response: {response}";
                    throw new B2c2RestException(message, e, guid);
                }

                throw new B2c2RestException(errorResponse, guid);
            }
        }
    }
}
