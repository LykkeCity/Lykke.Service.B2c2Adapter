using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.B2c2Client.Services
{
    public interface IB2c2RestService
    {
        Task<IReadOnlyDictionary<string, decimal>> GetBalance(CancellationToken ct = default(CancellationToken));

        //Task<MarginRequirements> GetMarginRequirements();

        //Task<IReadOnlyCollection<OpenPosition>> GetOpenPositions();

        Task<IReadOnlyCollection<Instrument>> GetInstruments(CancellationToken ct = default(CancellationToken));

        Task<RequestForQuoteResponse> RequestForQuote(RequestForQuoteRequest requestForQuoteRequest,
            CancellationToken ct = default(CancellationToken));

        Task<OrderResponse> PostOrder(OrderRequest orderRequest, CancellationToken ct = default(CancellationToken));

        //OrderResponse GetOrder(string clientOrderId);

        //Trade Trade(TradeRequest tradeRequest);
    }
}
