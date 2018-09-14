using System.Threading.Tasks;
using Autofac;
using Common;
using Lykke.Job.B2c2Adapter.Contract;

namespace Lykke.Service.B2c2Adapter.Domain.Services
{
    public interface IMyRabbitPublisher : IStartable, IStopable
    {
        Task PublishAsync(MyPublishedMessage message);
    }
}