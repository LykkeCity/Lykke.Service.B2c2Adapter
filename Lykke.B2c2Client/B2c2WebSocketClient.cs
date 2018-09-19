using System;
using System.Collections.Concurrent;
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
    public class B2c2WebSocketClient : IDisposable
    {
        private readonly string _baseUri = "wss://sandboxsocket.b2c2.net";
        private readonly string _authorizationToken;
        private readonly ILog _log;
        private ClientWebSocket _clientWebSocket;
        private readonly object _sync = new object();
        private readonly IDictionary<string, Func<PriceMessage, Task>> _subscriptions;
        private readonly IDictionary<string, IList<Func<PriceMessage, Task>>> _instrumentsHandlers;
        private readonly ConcurrentDictionary<string, string> _tradableInstruments;

        public B2c2WebSocketClient(string authorizationToken, ILogFactory logFactory)
        {
            _authorizationToken = authorizationToken;
            _log = logFactory.CreateLog(this);
            _subscriptions = new Dictionary<string, Func<PriceMessage, Task>>();
            _instrumentsHandlers = new Dictionary<string, IList<Func<PriceMessage, Task>>>();
            _tradableInstruments = new ConcurrentDictionary<string, string>();
        }

        public async Task ConnectAsync(CancellationToken ct = default(CancellationToken))
        {
            _log.Info("Attempt to establish a WebSocket connection.");

            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.Options.SetRequestHeader("Authorization", $"Token {_authorizationToken}");
            await _clientWebSocket.ConnectAsync(new Uri($"{_baseUri}/quotes"), ct).ConfigureAwait(false);

            if (_clientWebSocket.State != WebSocketState.Open)
                throw new Exception($"Could not establish WebSocket connection to {_baseUri}.");
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            _log.Info("Attempt to close a WebSocket connection.");

            if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure.", cancellationToken);
            }

            _log.Info("Connection to WebSocket was sucessfuly closed.");
        }

        public async Task SubscribeToOrderBookUpdatesAsync(SubscribeRequest subscribeRequest, Func<PriceMessage, Task> handler,
            CancellationToken ct = default(CancellationToken))
        {
            if (subscribeRequest == null) throw new NullReferenceException(nameof(subscribeRequest));
            if (handler == null) throw new NullReferenceException(nameof(handler));

            _log.Info($"Attempt to subscribe to order book updates, tag: {subscribeRequest.Tag}.");

            // lock(_sync) { ConnectIfNeeded(); }

            var request = StringToArraySegment(JsonConvert.SerializeObject(subscribeRequest));
            await _clientWebSocket.SendAsync(request, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

            lock (_sync)
            {
                _subscriptions[subscribeRequest.Tag] = handler;
            }
            
            // Listen for updates in another method
        }

        private async Task ListenToMessagesAsync(CancellationToken ct = default(CancellationToken))
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
                default:
                    // Clients are expected to ignore messages they do not support.
                    break;
            }
        }

        private void HandleTradableInstrumentMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                _log.Warning($"{nameof(ConnectResponse)}.{nameof(ConnectResponse.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<ConnectResponse>();
            foreach (var instrument in result.Instruments)
                _tradableInstruments[instrument] = instrument;
        }

        private void HandleSubscribeMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                lock (_sync)
                {
                    _subscriptions.Remove(jToken["tag"].Value<string>());
                }

                _log.Warning($"{nameof(SubscribeMessage)}.{nameof(SubscribeMessage.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<SubscribeMessage>();
            var key = result.Tag;
            lock (_sync)
            {
                if (_subscriptions.ContainsKey(key))
                {
                    var handler = _subscriptions[key];
                    _subscriptions.Remove(key);

                    var handlers = _instrumentsHandlers[result.Instrument];
                    if (handlers == null)
                        handlers = new List<Func<PriceMessage, Task>> { handler };
                    else
                        handlers.Add(handler);

                    _instrumentsHandlers[result.Instrument] = handlers;
                }
            }
        }

        private void HandlePriceMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                _log.Warning($"{nameof(SubscribeMessage)}.{nameof(SubscribeMessage.Success)} == false. {jToken}");
                return;
            }

            var result = jToken.ToObject<PriceMessage>();
            lock (_sync)
            {
                var handlers = _instrumentsHandlers[result.Instrument];
                foreach (var handler in handlers)
                    handler(result);
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
            }
        }

        #endregion
    }
}
