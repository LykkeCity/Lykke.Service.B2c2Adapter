using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.B2c2Adapter.Settings;

namespace Lykke.Service.B2c2Adapter.RabbitMq.Publishers
{
    public class OrderBookPublisher : IOrderBookPublisher, IStartable, IStopable
    {
        private readonly PublishingSettings _publishingSettings;
        private RabbitMqPublisher<OrderBook> _publisher;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;

        public OrderBookPublisher(
            PublishingSettings publishingSettings,
            ILogFactory logFactory)
        {
            _publishingSettings = publishingSettings;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
        }

        public void Start()
        {
            // NOTE: Read https://github.com/LykkeCity/Lykke.RabbitMqDotNetBroker/blob/master/README.md to learn
            // about RabbitMq subscriber configuration
            
            if (!_publishingSettings.Enabled)
                return;

            var settings = RabbitMqSubscriptionSettings
                .ForPublisher(_publishingSettings.ConnectionString, _publishingSettings.ExchangeName);

            _publisher = new RabbitMqPublisher<OrderBook>(_logFactory, settings)
                .SetSerializer(new JsonMessageSerializer<OrderBook>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .PublishSynchronously()
                .Start();
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }

        public async Task PublishAsync(OrderBook message)
        {
            if (_publisher == null || !_publishingSettings.Enabled)
                return;

            try
            {
                await _publisher.ProduceAsync(message);
            }
            catch (Exception e)
            {
                var logMessage = $"OrderBookPublisher.PublishAsync() exception: ${e}.";

                if (e.Message.Contains("isn't started yet"))
                    _log.Info(logMessage);
                else
                    _log.Warning(logMessage);
            }
        }
    }
}
