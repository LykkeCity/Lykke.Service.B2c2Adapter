using System.Threading.Tasks;
using Lykke.Sdk;
using Lykke.Service.B2c2Adapter.RabbitMq.Publishers;
using Lykke.Service.B2c2Adapter.Services;

namespace Lykke.Service.B2c2Adapter.Managers
{
    public class Manager : IStartupManager, IShutdownManager
    {
        private OrderBookPublisher OrderBookPublisher { get; set; }
        private TickPricePublisher TickPricePublisher { get; set; }
        private OrderBooksService OrderBooksService { get; set; }
        private ReportService ReportService { get; set; }

        public Task StartAsync()
        {
            OrderBookPublisher.Start();

            TickPricePublisher.Start();

            OrderBooksService.Start();

            ReportService.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            OrderBookPublisher.Stop();

            TickPricePublisher.Stop();

            OrderBooksService.Stop();

            ReportService.Stop();

            return Task.CompletedTask;
        }
    }
}
