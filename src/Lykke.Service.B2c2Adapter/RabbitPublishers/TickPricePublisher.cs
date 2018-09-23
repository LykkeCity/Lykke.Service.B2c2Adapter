using System.Threading.Tasks;
using Autofac;
using Common;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.B2c2Adapter.Settings;

namespace Lykke.Service.B2c2Adapter.RabbitPublishers
{
    public class TickPricePublisher : ITickPricePublisher, IStartable, IStopable
    {
        private readonly ILogFactory _logFactory;
        private readonly PublishingSettings _settting;
        private RabbitMqPublisher<TickPrice> _publisher;

        public TickPricePublisher(ILogFactory logFactory, PublishingSettings settting)
        {
            _logFactory = logFactory;
            _settting = settting;
        }

        public void Start()
        {
            // NOTE: Read https://github.com/LykkeCity/Lykke.RabbitMqDotNetBroker/blob/master/README.md to learn
            // about RabbitMq subscriber configuration

            var settings = RabbitMqSubscriptionSettings
                .ForPublisher(_settting.ConnectionString, _settting.ExchangeName);

            _publisher = new RabbitMqPublisher<TickPrice>(_logFactory, settings)
                .SetSerializer(new JsonMessageSerializer<TickPrice>())
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

        public async Task PublishAsync(TickPrice message)
        {
            await _publisher.ProduceAsync(message);
        }
    }
}
