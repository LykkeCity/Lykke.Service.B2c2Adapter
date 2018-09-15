using System.Collections.Generic;
using Lykke.B2c2Client.Models.RestApi;

namespace Lykke.B2c2Client.Services
{
    /// Works with rest api
    /// Must have 'Authorization' token
    /// Must contain: 'Content-Type: application/json;'
    public interface IRestApiService
    {
        IReadOnlyDictionary<string, decimal> GetBalance();

        MarginRequirements GetMarginRequirements();

        IReadOnlyCollection<OpenPosition> GetOpenPositions();

        IReadOnlyCollection<Instrument> GetInstruments();

        
        RequestForQuoteResponse RequestForQuote(RequestForQuoteRequest requestForQuoteRequest);

        OrderResponse PostOrder(OrderRequest orderRequest);

        OrderResponse GetOrder(string clientOrderId);

        Trade Trade(TradeRequest tradeRequest);

        /// Contract for difference
        IReadOnlyCollection<CfdTrade> TradeCfd(CfdTradeRequest cfqTradeRequest);

        /// Returns collection of regular and CFD trades
        IReadOnlyCollection<Trade> GetAllTrades(int offset = 0, int limit = 0);
    }
}
