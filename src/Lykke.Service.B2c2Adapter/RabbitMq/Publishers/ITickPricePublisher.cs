using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;

namespace Lykke.Service.B2c2Adapter.RabbitMq.Publishers
{
    public interface ITickPricePublisher
    {
        Task PublishAsync(TickPrice message);
    }
}
