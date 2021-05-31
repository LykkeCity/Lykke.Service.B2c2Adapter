using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Lykke.Service.B2c2Adapter.ZeroMq
{
    public class ZeroMqOrderPublisherDispatcher : BackgroundService
    {
        private readonly ZeroMqOrderBookPublisher _orderBookPublisher;

        public ZeroMqOrderPublisherDispatcher(ZeroMqOrderBookPublisher orderBookPublisher)
        {
            _orderBookPublisher = orderBookPublisher;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => _orderBookPublisher.StartAsync(stoppingToken, _ => {}), stoppingToken);
        }
    }
}
