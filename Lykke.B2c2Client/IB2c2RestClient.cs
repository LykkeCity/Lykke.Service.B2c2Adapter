using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.B2c2Client
{
    public interface IB2c2RestClient
    {
        Task<IReadOnlyDictionary<string, decimal>> BalanceAsync(CancellationToken ct = default(CancellationToken));

        //Task<MarginRequirements> GetMarginRequirements();

        //Task<IReadOnlyCollection<OpenPosition>> GetOpenPositions();

        Task<IReadOnlyCollection<Instrument>> InstrumentsAsync(CancellationToken ct = default(CancellationToken));

        Task<RequestForQuoteResponse> RequestForQuoteAsync(RequestForQuoteRequest requestForQuoteRequest,
            CancellationToken ct = default(CancellationToken));

        Task<OrderResponse> OrderAsync(OrderRequest orderRequest, CancellationToken ct = default(CancellationToken));

        //OrderResponse GetOrder(string clientOrderId);

        Task<Trade> TradeAsync(TradeRequest tradeRequest, CancellationToken ct = default(CancellationToken));
    }
}
