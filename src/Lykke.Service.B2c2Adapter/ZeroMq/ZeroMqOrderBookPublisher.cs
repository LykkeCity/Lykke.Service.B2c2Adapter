using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.Settings;
using Lykke.Service.B2c2Adapter.Utils;
using NetMQ;
using Swisschain.Hedger.Mixer.ApiClient.Extensions;
using Swisschain.Hedger.Mixer.ApiContract;

namespace Lykke.Service.B2c2Adapter.ZeroMq
{
    public class ZeroMqOrderBookPublisher
    {
        private readonly ConcurrentDictionary<string, OrderBook> _latestOrderBooks = new();
        private readonly ManualResetEventSlim _event = new(false);

        private readonly ILog _logger;
        private readonly ZeroMqPublisher _publisher;
        private readonly string _topicName;

        public ZeroMqOrderBookPublisher(
            ILogFactory logFactory,
            ZeroMqPublishingSettings orderBookPublishingSettings
            )
        {
            _topicName = orderBookPublishingSettings.TopicName;
            
            InternalMetrics.OrderBookOutDictionarySize.Set(0);
            
            _logger = logFactory.CreateLog(this);
            
            _publisher = new ZeroMqPublisher(logFactory, orderBookPublishingSettings.PublishingUrl);
        }
        
        public Task PublishAsync(Common.ExchangeAdapter.Contracts.OrderBook orderBook, string rawAssetPair)
        {
            var key = GetKey(orderBook.Source, orderBook.Asset);

            _latestOrderBooks[key] = Map(orderBook, rawAssetPair);
            InternalMetrics.OrderBookOutDictionarySize.Set(_latestOrderBooks.Count);
   
            _event.Set();
            
            return Task.CompletedTask;
        }

        private OrderBook Map(Common.ExchangeAdapter.Contracts.OrderBook orderBook, string rawAssetPair)
        {
            var orderBookAsset = rawAssetPair.Split("/");
            
            var mapped = new OrderBook
            {
                Source = orderBook.Source,
                Timestamp = orderBook.Timestamp.ToTimestamp(),
                AssetPair = new AssetPair {Base = orderBookAsset[0], Quote = orderBookAsset[1]},
            };

            foreach (var ask in orderBook.Asks)
            {
                mapped.Asks.Add(new LimitOrder
                {
                    Price = ask.Price.ToString(CultureInfo.InvariantCulture),
                    Volume = ask.Volume.ToString(CultureInfo.InvariantCulture)
                });
            }
            
            foreach (var bid in orderBook.Bids)
            {
                mapped.Bids.Add(new LimitOrder
                {
                    Price = bid.Price.ToString(CultureInfo.InvariantCulture),
                    Volume = bid.Volume.ToString(CultureInfo.InvariantCulture)
                });
            }

            return mapped;
        }

        public async Task StartAsync(CancellationToken ct, Action<OrderBook> callback)
        {
            _publisher.Start(publisher =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var orderBooks = _latestOrderBooks.Values.ToList();

                        _latestOrderBooks.Clear();

                        if (orderBooks.Count > 0)
                        {
                            var getOrderBooksResponse = new GetOrderBooksResponse();
                            getOrderBooksResponse.OrderBooks.AddRange(orderBooks);

                            try
                            {
                                var messageBytes = getOrderBooksResponse.ToByteArray();

                                publisher.SendMoreFrame(_topicName).SendFrame(messageBytes);

                                foreach (var orderBook in orderBooks)
                                {
                                    callback(orderBook);
                                }
                            }
                            catch (Exception exception)
                            {
                                _logger.Error(exception, "Something went wrong while broadcasting the message.");
                            }

                            InternalMetrics.MessagesOutCount.Inc();
                            InternalMetrics.OutBatchSize.Set(orderBooks.Count);

                            foreach (var orderBook in orderBooks)
                            {
                                if (orderBook.Bids.Any())
                                {
                                    InternalMetrics.OrderBookOutSidePrice
                                        .WithLabels(orderBook.Source, orderBook.AssetPair.ToAssetPairString(), "bid")
                                        .Set(double.Parse(orderBook.Bids.First().Price));
                                }

                                if (orderBook.Asks.Any())
                                {
                                    InternalMetrics.OrderBookOutSidePrice
                                        .WithLabels(orderBook.Source, orderBook.AssetPair.ToAssetPairString(), "ask")
                                        .Set(double.Parse(orderBook.Asks.First().Price));
                                }
                            }
                        }

                        if (_latestOrderBooks.IsEmpty)
                        {
                            _event.Wait(ct);
                            _event.Reset();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Something went wrong in {nameof(ZeroMqOrderBookPublisher)}.{nameof(StartAsync)}().");
                    }
                }
            });
        }
        
        private string GetKey(string source, string assetPair) => $"{source}-{assetPair}";
    }
}
