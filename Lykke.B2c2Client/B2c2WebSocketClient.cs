using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<(string, bool), Func<OrderBookResponse, Task>> _subscriptions;
        private readonly ConcurrentDictionary<string, string> _tradableInstruments;

        public B2c2WebSocketClient(string authorizationToken, ILogFactory logFactory)
        {
            _authorizationToken = authorizationToken;
            _log = logFactory.CreateLog(this);
            _subscriptions = new ConcurrentDictionary<(string, bool), Func<OrderBookResponse, Task>>();
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

        public async Task SubscribeToOrderBookUpdatesAsync(SubscribeRequest subscribeRequest, Func<OrderBookResponse, Task> handler,
            CancellationToken ct = default(CancellationToken))
        {
            if (subscribeRequest == null) throw new NullReferenceException(nameof(subscribeRequest));
            if (handler == null) throw new NullReferenceException(nameof(handler));

            _log.Info($"Attempt to subscribe to order book updates, tag: {subscribeRequest.Tag}.");

            // lock(_sync) { ConnectIfNeeded(); }

            var request = StringToArraySegment(JsonConvert.SerializeObject(subscribeRequest));
            await _clientWebSocket.SendAsync(request, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

            _subscriptions[(subscribeRequest.Tag, false)] = handler;

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

                    await HandleWebSocketMessageAsync(jsonMessage);
                }
            }
        }

        private async Task HandleWebSocketMessageAsync(string jsonMessage)
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
                throw new B2c2WebSocketException($"{nameof(ConnectResponse)}.{nameof(ConnectResponse.Success)} == false.");

            var result = jToken.ToObject<ConnectResponse>();
            foreach (var instrument in result.Instruments)
                _tradableInstruments[instrument] = instrument;
        }

        private void HandleSubscribeMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
                throw new B2c2WebSocketException($"{nameof(SubscribeResponse)}.{nameof(SubscribeResponse.Success)} == false.");
        }

        private void HandlePriceMessage(JToken jToken)
        {

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
