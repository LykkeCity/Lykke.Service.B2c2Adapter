using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.B2c2Client.Exceptions;
using Lykke.B2c2Client.Models.WebSocket;
using Lykke.Common.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.B2c2Client
{
    public class B2c2WebSocketClient : IB2c2WebSocketClient, IDisposable
    {
        private readonly string _baseUri;
        private readonly string _authorizationToken;
        private readonly ILog _log;
        private ClientWebSocket _clientWebSocket;
        private readonly object _sync = new object();
        private readonly IDictionary<string, Func<PriceMessage, Task>> _subscriptions;
        private readonly IDictionary<string, Func<PriceMessage, Task>> _instrumentsHandlers;
        private readonly IList<string> _tradableInstruments;
        private readonly CancellationTokenSource _tokenSource;

        public B2c2WebSocketClient(string url, string authorizationToken, ILogFactory logFactory)
        {
            _baseUri = url[url.Length-1] == '/' ? url.Substring(0, url.Length - 1) : url;
            _authorizationToken = authorizationToken;
            _log = logFactory.CreateLog(this);
            _clientWebSocket = new ClientWebSocket();
            _subscriptions = new Dictionary<string, Func<PriceMessage, Task>>();
            _instrumentsHandlers = new Dictionary<string, Func<PriceMessage, Task>>();
            _tradableInstruments = new List<string>();
            _tokenSource = new CancellationTokenSource();
        }

        public async Task ConnectAsync(CancellationToken ct = default(CancellationToken))
        {
            _log.Info("Attempt to establish a WebSocket connection.");

            _clientWebSocket.Options.SetRequestHeader("Authorization", $"Token {_authorizationToken}");
            await _clientWebSocket.ConnectAsync(new Uri($"{_baseUri}/quotes"), ct).ConfigureAwait(false);

            if (_clientWebSocket.State != WebSocketState.Open)
                throw new Exception($"Could not establish WebSocket connection to {_baseUri}.");

            // Listen for messages in separate thread
            #pragma warning disable 4014
            Task.Run(async () =>
                {
                    await HandleMessagesCycleAsync(_tokenSource.Token);
                }, _tokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _log.Error(t.Exception, "Something went wrong in subscription thread.");
                }, default(CancellationToken));
            #pragma warning restore 4014
        }

        public async Task DisconnectAsync(CancellationToken ct = default(CancellationToken))
        {
            _log.Info("Attempt to close a WebSocket connection.");

            if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure.", ct);
            }

            _subscriptions.Clear();
            _instrumentsHandlers.Clear();
            _tradableInstruments.Clear();

            _log.Info("Connection to WebSocket was sucessfuly closed.");
        }

        public async Task SubscribeAsync(string instrument, int[] levels, Func<PriceMessage, Task> handler,
            CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(instrument)) throw new ArgumentOutOfRangeException(nameof(instrument));
            //if (levels.Length < 1 || levels.Length > 2) throw new ArgumentOutOfRangeException($"{nameof(levels)}. Minimum levels - 1, maximum - 2.");
            if (handler == null) throw new NullReferenceException(nameof(handler));

            var subscribeRequest = new SubscribeRequest
            {
                Instrument = instrument,
                Levels = levels,
                Tag = Guid.NewGuid().ToString()
            };

            lock (_sync)
            {
                if (_subscriptions.ContainsKey(instrument)
                    || _instrumentsHandlers.ContainsKey(instrument))
                    throw new B2c2WebSocketException($"Subscription to {instrument} is already exists.");
            }

            if (_clientWebSocket.State == WebSocketState.None)
                await ConnectAsync(ct);

            _log.Info($"Attempt to subscribe to order book updates, instrument: {subscribeRequest.Instrument}.");

            var request = StringToArraySegment(JsonConvert.SerializeObject(subscribeRequest));
            await _clientWebSocket.SendAsync(request, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

            lock (_sync)
            {
                _subscriptions[subscribeRequest.Instrument] = handler;
            }
        }

        public async Task UnsubscribeAsync(string instrument, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(instrument)) throw new NullReferenceException(nameof(instrument));

            lock (_sync)
            {
                if (!_instrumentsHandlers.ContainsKey(instrument))
                    throw new B2c2WebSocketException($"Subscription to {instrument} is not existed.");
            }

            if (_clientWebSocket.State != WebSocketState.Open)
                throw new B2c2WebSocketException($"WebSocketState is not 'Open' - {_clientWebSocket.State}.");

            _log.Info($"Attempt to unsubscribe from order book updates, instrument: {instrument}.");

            var unsubscribeRequest = new UnsubscribeRequest
            {
                Instrument = instrument,
                Tag = Guid.NewGuid().ToString()
            };

            var request = StringToArraySegment(JsonConvert.SerializeObject(unsubscribeRequest));
            await _clientWebSocket.SendAsync(request, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }

        private async Task HandleMessagesCycleAsync(CancellationToken ct)
        {
            while (_clientWebSocket.State == WebSocketState.Open)
            {
                using (var stream = new MemoryStream(8192))
                {
                    var receiveBuffer = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        receiveResult = await _clientWebSocket.ReceiveAsync(receiveBuffer, ct);
                        await stream.WriteAsync(receiveBuffer.Array, receiveBuffer.Offset, receiveResult.Count, ct);
                    } while (!receiveResult.EndOfMessage);

                    var messageBytes = stream.ToArray();
                    var jsonMessage = Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length);

                    HandleWebSocketMessageAsync(jsonMessage);
                }
            }
        }

        private void HandleWebSocketMessageAsync(string jsonMessage)
        {
            var jToken = JToken.Parse(jsonMessage);
            var type = jToken["event"]?.Value<string>();

            switch (type)
            {
                case "tradable_instruments":
                    HandleTradableInstrumentMessage(jToken);
                    break;
                case "subscribe":
                    HandleSubscribeMessage(jToken);
                    break;
                case "price":
                    HandlePriceMessage(jToken);
                    break;
                case "unsubscribe":
                    HandleUnsubscribeMessage(jToken);
                    break;
            }
        }

        private void HandleTradableInstrumentMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                lock (_sync)
                {
                    var key = jToken["instrument"].Value<string>();
                    if (_subscriptions.ContainsKey(key))
                        _subscriptions.Remove(key);
                }
                _log.Error($"{nameof(ConnectResponse)}.{nameof(ConnectResponse.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<ConnectResponse>();
            foreach (var instrument in result.Instruments)
                _tradableInstruments.Add(instrument);
        }

        private void HandleSubscribeMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                lock (_sync)
                {
                    _subscriptions.Remove(jToken["instrument"].Value<string>());
                }

                _log.Error($"{nameof(SubscribeMessage)}.{nameof(SubscribeMessage.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<SubscribeMessage>();
            var instrument = result.Instrument;
            lock (_sync)
            {
                if (_subscriptions.ContainsKey(instrument))
                {
                    var handler = _subscriptions[instrument];
                    _subscriptions.Remove(instrument);

                    if (_instrumentsHandlers.ContainsKey(result.Instrument))
                        _log.Error($"Attempt to second subscription to {result.Instrument}.");

                    _instrumentsHandlers[result.Instrument] = handler;
                }
                else
                {
                    _log.Error($"Subscriptions doesn't have element with {result.Instrument}.");
                }
            }
        }

        private void HandlePriceMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                _log.Error($"{nameof(SubscribeMessage)}.{nameof(SubscribeMessage.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<PriceMessage>();
            lock (_sync)
            {
                var handler = _instrumentsHandlers[result.Instrument];
                handler(result);
            }
        }

        private void HandleUnsubscribeMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                _log.Error($"{nameof(UnsubscribeMessage)}.{nameof(UnsubscribeMessage.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<UnsubscribeMessage>();
            var instrument = result.Instrument;
            lock (_sync)
            {
                if (!_subscriptions.ContainsKey(instrument))
                    _log.Error($"Can't unsubscribe from '{instrument}', handler is not existed. {jToken}");

                _subscriptions.Remove(instrument);
            }
        }

        private ArraySegment<byte> StringToArraySegment(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var messageArraySegment = new ArraySegment<byte>(messageBytes);
            return messageArraySegment;
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~B2c2WebSocketClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (_clientWebSocket != null)
                {
                    _clientWebSocket.Abort();
                    _clientWebSocket.Dispose();
                    _clientWebSocket = null;
                }

                if (_tokenSource != null && _tokenSource.Token.CanBeCanceled)
                {
                    _tokenSource.Cancel();
                    _tokenSource.Dispose();
                }
            }
        }

        #endregion
    }
}
