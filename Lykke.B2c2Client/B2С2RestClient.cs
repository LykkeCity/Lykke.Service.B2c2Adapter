using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.B2c2Client.Exceptions;
using Lykke.B2c2Client.Models.Rest;
using Lykke.B2c2Client.Settings;
using Lykke.Common.Log;
using Newtonsoft.Json;

namespace Lykke.B2c2Client
{
    public class B2С2RestClient : IB2С2RestClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILog _log;

        public B2С2RestClient(B2C2ClientSettings settings, ILogFactory logFactory)
        {
            if (settings == null) throw new NullReferenceException(nameof(settings));
            var url = settings.Url;
            var authorizationToken = settings.AuthorizationToken;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new ArgumentOutOfRangeException(nameof(url));
            if (string.IsNullOrWhiteSpace(authorizationToken)) throw new ArgumentOutOfRangeException(nameof(authorizationToken));
            if (logFactory == null) throw new NullReferenceException(nameof(logFactory));

            url = url[url.Length - 1] == '/' ? url.Substring(0, url.Length - 1) : url;
            _httpClient = new HttpClient { BaseAddress = new Uri(url) };
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {authorizationToken}");
            _log = logFactory.CreateLog(this);
        }

        public async Task<IReadOnlyDictionary<string, decimal>> BalanceAsync(CancellationToken ct = default(CancellationToken))
        {
            var requestId = Guid.NewGuid();

            _log.Info("balance - request", requestId);

            var responseStr = string.Empty;

            try
            {
                using (var response = await _httpClient.GetAsync("balance/", ct))
                {
                    var status = response.StatusCode;
                    
                    responseStr = await response.Content.ReadAsStringAsync();
                    
                    _log.Info("balance - response", new { RequestId = requestId, Response = responseStr });

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "balance - response exception", new { RequestId = requestId, Response = responseStr });

                throw;
            }
        }

        public async Task<IReadOnlyCollection<Instrument>> InstrumentsAsync(CancellationToken ct = default(CancellationToken))
        {
            var requestId = Guid.NewGuid();

            _log.Info("instruments - request", requestId);

            var responseStr = string.Empty;

            try
            {
                using (var response = await _httpClient.GetAsync("instruments/", ct))
                {
                    var status = response.StatusCode;

                    responseStr = await response.Content.ReadAsStringAsync();
                    
                    _log.Info("instruments - response", new { RequestId = requestId, Response = responseStr });

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<IReadOnlyCollection<Instrument>>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "instruments - response exception", new { RequestId = requestId, Response = responseStr });

                throw;
            }
        }

        public async Task<RequestForQuoteResponse> RequestForQuoteAsync(RequestForQuoteRequest requestForQuoteRequest, CancellationToken ct = default(CancellationToken))
        {
            if (requestForQuoteRequest == null) throw new ArgumentNullException(nameof(requestForQuoteRequest));

            var requestId = Guid.NewGuid();

            _log.Info("request for quote - request", requestForQuoteRequest);

            var responseStr = string.Empty;

            try
            {
                using (var response = await _httpClient.PostAsJsonAsync("request_for_quote/", requestForQuoteRequest, ct))
                {
                    var status = response.StatusCode;

                    responseStr = await response.Content.ReadAsStringAsync();
                    
                    _log.Info("request for quote - response", new { RequestId = requestId, Response = responseStr });

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
                _log.Error(e, "request for quote - response exception", new { RequestId = requestId, Response = responseStr });
                
                throw;
            }
        }

        public async Task<OrderResponse> OrderAsync(OrderRequest orderRequest, CancellationToken ct = default(CancellationToken))
        {
            if (orderRequest == null) throw new ArgumentNullException(nameof(orderRequest));

            var requestId = Guid.NewGuid();

            _log.Info("order - request", orderRequest);

            var responseStr = string.Empty;

            try
            {
                using (var response = await _httpClient.PostAsJsonAsync("order/", orderRequest, ct))
                {
                    var status = response.StatusCode;

                    responseStr = await response.Content.ReadAsStringAsync();

                    _log.Info("order - response", new { RequestId = requestId, Response = responseStr });

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
                _log.Error(e, "order - response exception", new { RequestId = requestId, Response = responseStr });

                throw;
            }
        }

        public async Task<Trade> TradeAsync(TradeRequest tradeRequest, CancellationToken ct = default(CancellationToken))
        {
            if (tradeRequest == null) throw new ArgumentNullException(nameof(tradeRequest));

            var requestId = Guid.NewGuid();

            _log.Info("trade - request", tradeRequest);

            var responseStr = string.Empty;

            try
            {
                using (var response = await _httpClient.PostAsJsonAsync("trade/", tradeRequest, ct))
                {
                    var status = response.StatusCode;

                    responseStr = await response.Content.ReadAsStringAsync();

                    _log.Info("trade - response", new { RequestId = requestId, Response = responseStr });

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<Trade>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "trade - response exception", new { RequestId = requestId, Response = responseStr });

                throw;
            }
        }

        public async Task<List<TradeLog>> GetTradeHistoryAsync(int offset = 0, int limit = 50, CancellationToken ct = default(CancellationToken))
        {
            var requestId = Guid.NewGuid();

            _log.Info("trade history - request", requestId);

            var responseStr = string.Empty;

            try
            {
                using (var response = await _httpClient.GetAsync($"trade/?offset={offset}&limit={limit}", ct))
                {
                    var status = response.StatusCode;

                    responseStr = await response.Content.ReadAsStringAsync();

                    _log.Info("trade history - response", new { RequestId = requestId, Response = responseStr });

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<List<TradeLog>>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "trade history - response exception", new {
                    RequestId = requestId,
                    Response = responseStr
                });

                throw;
            }
        }

        public async Task<List<LedgerLog>> GetLedgerHistoryAsync(int offset = 0, int limit = 50, CancellationToken ct = default(CancellationToken))
        {
            var requestId = Guid.NewGuid();
            _log.Info("ledger history - request", requestId);

            try
            {
                using (var response = await _httpClient.GetAsync($"ledger/?offset={offset}&limit={limit}", ct))
                {
                    var status = response.StatusCode;

                    var responseStr = await response.Content.ReadAsStringAsync();
                    _log.Info("ledger history - response", requestId);

                    CheckForError(responseStr, status, requestId);

                    var result = JsonConvert.DeserializeObject<List<LedgerLog>>(responseStr);

                    return result;
                }
            }
            catch (Exception e)
            {
                _log.Info($"ledger history - response exception: {e}", requestId);
                throw;
            }
        }

        private void CheckForError(string response, HttpStatusCode status, Guid guid)
        {
            if (!response.Contains("errors"))
                return;

            ErrorResponse errorResponse;

            try
            {
                errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response);

                errorResponse.Status = status;
            }
            catch (Exception e)
            {
                var message = $"Can't deserialize error response, status: {(int)status} {status}, guid: {guid}, response: {response}";

                throw new B2c2RestException(message, e, guid);
            }

            throw new B2c2RestException(errorResponse, guid);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
