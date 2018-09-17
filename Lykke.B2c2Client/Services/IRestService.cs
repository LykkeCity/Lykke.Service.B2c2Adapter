using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.B2c2Client.Services
{
    /// Works with rest api
    /// Must have 'Authorization' token
    /// Must contain: 'Content-Type: application/json;'
    public interface IRestService
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

        /// Contract for difference
        //IReadOnlyCollection<CfdTrade> TradeCfd(CfdTradeRequest cfqTradeRequest);

        /// Returns collection of regular and CFD trades
        //IReadOnlyCollection<Trade> GetAllTrades(int offset = 0, int limit = 0);
    }
}
