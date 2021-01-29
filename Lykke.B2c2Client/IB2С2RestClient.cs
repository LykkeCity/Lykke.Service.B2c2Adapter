using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.B2c2Client
{
    public interface IB2С2RestClient : IDisposable
    {
        Task<IReadOnlyDictionary<string, decimal>> BalanceAsync(CancellationToken ct = default(CancellationToken));

        Task<IReadOnlyCollection<Instrument>> InstrumentsAsync(CancellationToken ct = default(CancellationToken));

        Task<RequestForQuoteResponse> RequestForQuoteAsync(RequestForQuoteRequest requestForQuoteRequest,
            CancellationToken ct = default(CancellationToken));

        Task<OrderResponse> OrderAsync(OrderRequest orderRequest, CancellationToken ct = default(CancellationToken));

        Task<Trade> TradeAsync(TradeRequest tradeRequest, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// Get a list of all your executed trades.
        /// </summary>
        /// <param name="offset">Skip the amount of trades before returning results (default: 0)</param>
        /// <param name="limit">Number of results (default: 50, max: 100)</param>
        /// <returns></returns>
        Task<List<TradeLog>> GetTradeHistoryAsync(int offset = 0, int limit = 50, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// Get a list of all entries affecting your balance, such as trade legs and settlements.
        /// </summary>
        /// <param name="offset">Skip the amount of ledgers before returning results (default: 0)</param>
        /// <param name="limit">Number of results (default: 50, max: 100)</param>
        /// <returns></returns>
        Task<List<LedgerLog>> GetLedgerHistoryAsync(int offset = 0, int limit = 50, CancellationToken ct = default(CancellationToken));

        Task<PaginationResponse<List<LedgerLog>>> GetLedgerHistoryAsync(LedgersRequest request, CancellationToken ct = default(CancellationToken));
        Task<PaginationResponse<List<TradeLog>>> GetTradeHistoryAsync(TradesHistoryRequest request, CancellationToken ct = default(CancellationToken));
    }
}
