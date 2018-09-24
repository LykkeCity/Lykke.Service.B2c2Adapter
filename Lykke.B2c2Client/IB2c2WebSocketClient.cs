using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.B2c2Client.Models.WebSocket;

namespace Lykke.B2c2Client
{
    public interface IB2c2WebSocketClient : IDisposable
    {
        Task SubscribeAsync(string instrument, int[] levels, Func<PriceMessage, Task> handler,
            CancellationToken ct = default(CancellationToken));

        Task UnsubscribeAsync(string instrument, CancellationToken ct = default(CancellationToken));
    }
}
