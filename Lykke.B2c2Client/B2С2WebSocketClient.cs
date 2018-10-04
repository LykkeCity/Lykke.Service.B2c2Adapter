using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.B2c2Client.Exceptions;
using Lykke.B2c2Client.Models.WebSocket;
using Lykke.B2c2Client.Settings;
using Lykke.Common.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.B2c2Client
{
    public class B2С2WebSocketClient : IB2С2WebSocketClient
    {
        private readonly TimeSpan _timeOut = new TimeSpan(0, 0, 0, 10);
        private readonly string _baseUri;
        private readonly string _authorizationToken;
        private readonly ILog _log;
        private readonly object _syncWebSocket = new object();
        private ClientWebSocket _clientWebSocket;
        private readonly object _sync = new object();
        private readonly ConcurrentDictionary<string, Subscription> _awaitingSubscriptions;
        private readonly ConcurrentDictionary<string, Func<PriceMessage, Task>> _instrumentsHandlers;
        private readonly ConcurrentDictionary<string, decimal[]> _instrumentsLevels;
        private readonly ConcurrentDictionary<string, Subscription> _awaitingUnsubscriptions;
        private readonly IList<string> _tradableInstruments;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public B2С2WebSocketClient(B2C2ClientSettings settings, ILogFactory logFactory)
        {
            if (settings == null) throw new NullReferenceException(nameof(settings));
            var url = settings.Url;
            var authorizationToken = settings.AuthorizationToken;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new ArgumentOutOfRangeException(nameof(url));
            if (string.IsNullOrWhiteSpace(authorizationToken)) throw new ArgumentOutOfRangeException(nameof(authorizationToken));
            if (logFactory == null) throw new NullReferenceException(nameof(logFactory));

            _baseUri = url[url.Length-1] == '/' ? url.Substring(0, url.Length - 1) : url;
            _authorizationToken = authorizationToken;
            _log = logFactory.CreateLog(this);
            _clientWebSocket = new ClientWebSocket();
            _awaitingSubscriptions = new ConcurrentDictionary<string, Subscription>();
            _instrumentsHandlers = new ConcurrentDictionary<string, Func<PriceMessage, Task>>();
            _instrumentsLevels = new ConcurrentDictionary<string, decimal[]>();
            _awaitingUnsubscriptions = new ConcurrentDictionary<string, Subscription>();
            _tradableInstruments = new List<string>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task SubscribeAsync(string instrument, decimal[] levels, Func<PriceMessage, Task> handler,
            CancellationToken ct = default(CancellationToken))
        {
            ThrowIfSubscriptionIsAlreadyExist(instrument);

            ConnectIfNeeded(ct);

            var tag = Guid.NewGuid().ToString();

            _log.Info($"Attempt to subscribe to order book updates, instrument: '{instrument}'.", tag);

            var subscribeRequest = new SubscribeRequest { Instrument = instrument, Levels = levels, Tag = tag };
            SendMessageToWebSocket(subscribeRequest, ct).GetAwaiter().GetResult();

            // Save subscription state
            var taskCompletionSource = new TaskCompletionSource<int>();
            lock (_sync)
            {
                _awaitingSubscriptions[instrument] = new Subscription(tag, taskCompletionSource, handler);
                _instrumentsLevels[instrument] = levels;
            }

            var successTask = Task.WhenAny(taskCompletionSource.Task, Task.Delay(_timeOut, ct)).GetAwaiter().GetResult();

            if (successTask != taskCompletionSource.Task)
            {
                throw new B2c2WebSocketException($"Subscription timeout for {instrument}.");
            }

            return taskCompletionSource.Task;
        }

        public Task UnsubscribeAsync(string instrument, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(instrument)) throw new NullReferenceException(nameof(instrument));

            var tag = Guid.NewGuid().ToString();

            _log.Info($"Attempt to subscribe to order book updates, instrument: '{instrument}'.", tag);

            ThrowIfSubscriptionDeosNotExistOrUnsubscriptionAlreadyExists(instrument);

            var unsubscribeRequest = new UnsubscribeRequest { Instrument = instrument, Tag = tag };
            SendMessageToWebSocket(unsubscribeRequest, ct).GetAwaiter().GetResult();

            // Save unsubscription state
            var taskCompletionSource = new TaskCompletionSource<int>();
            lock (_sync)
            {
                _awaitingUnsubscriptions[instrument] = new Subscription(tag, taskCompletionSource);
            }

            var successTask = Task.WhenAny(taskCompletionSource.Task, Task.Delay(_timeOut, ct)).GetAwaiter().GetResult();

            if (successTask != taskCompletionSource.Task)
            {
                throw new B2c2WebSocketException($"Unsubscription timeout for {instrument}.");
            }

            return taskCompletionSource.Task;
        }

        private void Connect(CancellationToken ct = default(CancellationToken))
        {
            _log.Info("Attempt to establish a WebSocket connection.");

            _clientWebSocket.Options.SetRequestHeader("Authorization", $"Token {_authorizationToken}");
            _clientWebSocket.ConnectAsync(new Uri($"{_baseUri}/quotes"), ct).ConfigureAwait(false).GetAwaiter().GetResult();

            if (_clientWebSocket.State != WebSocketState.Open)
                throw new Exception($"Could not establish WebSocket connection to {_baseUri}.");

            // Listen for messages in separate io thread
            Task.Run(async () =>
                {
                    await HandleMessagesCycleAsync(_cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _log.Error(t.Exception, "Something went wrong in subscription thread.");
                }, default(CancellationToken));
        }
        
        private Task HandleMessagesCycleAsync(CancellationToken ct)
        {
            while (_clientWebSocket.State == WebSocketState.Open)
            {
                using (var stream = new MemoryStream(8192))
                {
                    var receiveBuffer = new ArraySegment<byte>(new byte[1024]);
                    try
                    {
                        lock (_syncWebSocket)
                        {
                            WebSocketReceiveResult receiveResult;
                            do
                            {
                                receiveResult = _clientWebSocket.ReceiveAsync(receiveBuffer, ct).GetAwaiter().GetResult();
                                stream.WriteAsync(receiveBuffer.Array, receiveBuffer.Offset, receiveResult.Count, ct).GetAwaiter().GetResult();
                            } while (!receiveResult.EndOfMessage);
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error(e, "Error while processing a message from websocket.");
                    }

                    var messageBytes = stream.ToArray();
                    var jsonMessage = Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length);

                    HandleWebSocketMessageAsync(jsonMessage);
                }
            }

            return Task.CompletedTask;
        }

        private void HandleWebSocketMessageAsync(string jsonMessage)
        {
            JToken jToken = null;
            var type = "";
            try
            {
                if (string.IsNullOrWhiteSpace(jsonMessage))
                {
                    _log.Warning($"Empty message received: {jsonMessage}.");
                    return;
                }

                jToken = JToken.Parse(jsonMessage);
                type = jToken["event"]?.Value<string>();

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
                    default:
                        _log.Warning($"Strange type of message: {type}.");
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error(e, $"Type: {type}, message: {jToken}.");
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
                _tradableInstruments.Add(instrument);
        }

        private void HandleSubscribeMessage(JToken jToken)
        {
            var tag = jToken["tag"].Value<string>();
            if (jToken["success"]?.Value<bool>() == false)
            {
                lock (_sync)
                {
                    var instrument = _awaitingSubscriptions.Where(x => x.Value.Tag == tag).Select(x => x.Key).Single();
                    _awaitingSubscriptions.TryRemove(instrument, out var value);
                    value?.TaskCompletionSource.TrySetException(
                        new B2c2WebSocketException($"{nameof(SubscribeMessage)}.{nameof(SubscribeMessage.Success)} == false. {jToken}"));

                    _log.Warning($"Failed to subscribe to {instrument}.");
                }

                return;
            }
            
            var result = jToken.ToObject<SubscribeMessage>();
            lock (_sync)
            {
                var instrument = result.Instrument;
                if (!_awaitingSubscriptions.ContainsKey(instrument))
                    _log.Warning($"Subscriptions doesn't have element with '{instrument}.", tag);

                _awaitingSubscriptions.Remove(instrument, out var subscription);
                
                if (_instrumentsHandlers.ContainsKey(instrument))
                    subscription.TaskCompletionSource.TrySetException(new B2c2WebSocketException($"Attempt to second subscription to {instrument}."));

                _instrumentsHandlers[instrument] = subscription.Function;

                subscription.TaskCompletionSource.TrySetResult(0);

                _log.Info($"Subscribed to {instrument}.");
            }
        }

        private void HandlePriceMessage(JToken jToken)
        {
            if (jToken["success"]?.Value<bool>() == false)
            {
                var errorResponse = jToken.ToObject<ErrorResponse>();

                var message = $"{nameof(PriceMessage)}.{nameof(PriceMessage.Success)} == false. {jToken}";
                if (errorResponse.Code == ErrorCode.NotAbleToQuoteAtTheMoment)
                    _log.Info(message);
                else
                    _log.Warning(message);

                return;
            }

            var result = jToken.ToObject<PriceMessage>();

            Func<PriceMessage, Task> handler;
            lock (_sync)
            {
                handler = _instrumentsHandlers[result.Instrument];
            }
            try
            {
                handler(result).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _log.Warning("Handler (OrderBooksService) failed.", e);
            }
        }

        private void HandleUnsubscribeMessage(JToken jToken)
        {
            var tag = jToken["tag"].Value<string>();
            if (jToken["success"]?.Value<bool>() == false)
            {
                lock (_sync)
                {
                    var instrument = _awaitingUnsubscriptions.Where(x => x.Value.Tag == tag).Select(x => x.Key).Single();
                    _awaitingUnsubscriptions.Remove(instrument, out var value);
                    value.TaskCompletionSource.TrySetException(
                        new B2c2WebSocketException($"{nameof(UnsubscribeMessage)}.{nameof(UnsubscribeMessage.Success)} == false. {jToken}"));

                    _log.Warning($"Failed to unsubscribe from {instrument}.");
                }

                return;
            }

            var result = jToken.ToObject<UnsubscribeMessage>();
            lock (_sync)
            {
                var instrument = jToken["instrument"].Value<string>();
                if (!_awaitingUnsubscriptions.ContainsKey(instrument))
                    _log.Warning($"Can't unsubscribe from '{instrument}', subscription does not exist. {jToken}", tag);

                _awaitingUnsubscriptions.Remove(instrument, out var subscription);

                if (_instrumentsHandlers.ContainsKey(result.Instrument))
                    subscription.TaskCompletionSource.TrySetException(
                        new B2c2WebSocketException($"Attempt to second subscription to {result.Instrument}."));

                _instrumentsHandlers.Remove(instrument, out _);

                subscription.TaskCompletionSource.TrySetResult(0);

                _log.Info($"Unsubscribed from {instrument}.");
            }
        }

        private static ArraySegment<byte> StringToArraySegment(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var messageArraySegment = new ArraySegment<byte>(messageBytes);
            return messageArraySegment;
        }

        private bool IsSubscriptionInProgress(string instrument)
        {
            lock (_sync)
            {
                return _awaitingSubscriptions.ContainsKey(instrument)
                      || _instrumentsHandlers.ContainsKey(instrument);
            }
        }

        private void ThrowIfSubscriptionIsAlreadyExist(string instrument)
        {
            if (IsSubscriptionInProgress(instrument))
                throw new B2c2WebSocketException($"Subscription to '{instrument}' is already existed.");
        }

        private void ThrowIfSubscriptionDeosNotExistOrUnsubscriptionAlreadyExists(string instrument)
        {
            lock (_sync)
            {
                if (!_instrumentsHandlers.ContainsKey(instrument))
                    throw new B2c2WebSocketException($"Subscription to {instrument} does not exist.");
                if (_awaitingUnsubscriptions.ContainsKey(instrument))
                    throw new B2c2WebSocketException($"Unsubscription to '{instrument}' is already exist.");
            }
        }

        private Task SendMessageToWebSocket(IRequest request, CancellationToken ct = default(CancellationToken))
        {
            try
            {
                lock (_syncWebSocket)
                {
                    var requestSegment = StringToArraySegment(JsonConvert.SerializeObject(request));
                    _clientWebSocket.SendAsync(requestSegment, WebSocketMessageType.Text, true, ct).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                throw new B2c2WebSocketException(
                    "Something went wrong while sending a message to the web socket, see InternalException.", e);
            }

            return Task.CompletedTask;
        }

        private void ConnectIfNeeded(CancellationToken ct = default(CancellationToken))
        {
            if (_clientWebSocket.State == WebSocketState.None)
                Connect(ct);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~B2С2WebSocketClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            
            if (_clientWebSocket != null)
            {
                _clientWebSocket?.Abort();
                _clientWebSocket?.Dispose();
                _clientWebSocket = null;
            }

            if (_cancellationTokenSource != null && _cancellationTokenSource.Token.CanBeCanceled)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
        }

        #endregion

        private class Subscription
        {
            public string Tag { get; }

            public TaskCompletionSource<int> TaskCompletionSource { get; }

            public Func<PriceMessage, Task> Function { get; }

            public Subscription(string tag, TaskCompletionSource<int> taskCompletionSource, Func<PriceMessage, Task> function)
            {
                Tag = tag;
                TaskCompletionSource = taskCompletionSource;
                Function = function;
            }

            public Subscription(string tag, TaskCompletionSource<int> taskCompletionSource)
            {
                Tag = tag;
                TaskCompletionSource = taskCompletionSource;
            }
        }
    }
}
