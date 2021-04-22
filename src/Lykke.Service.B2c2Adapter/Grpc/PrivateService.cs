using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Swisschain.Liquidity.ApiContract;
using Trade = Swisschain.Liquidity.ApiContract.Trade;

namespace Lykke.Service.B2c2Adapter.Grpc
{
    public class PrivateService : PrivateGrpc.PrivateGrpcBase
    {
        private readonly IB2С2RestClient _b2C2RestClient;

        public PrivateService(IB2С2RestClient b2C2RestClient)
        {
            _b2C2RestClient = b2C2RestClient;
        }

        public override async Task<GetBalanceResponse> GetBalance(GetBalanceRequest request, ServerCallContext context)
        {
            var balances = await _b2C2RestClient.BalanceAsync(context.CancellationToken);
            var result = new GetBalanceResponse {Timestamp = DateTime.UtcNow.ToTimestamp()};
            result.AssetBalances.AddRange(Map(balances));

            return result;
        }

        public override async Task<ExecuteMarketOrderResponse> ExecuteMarketOrder(MarketOrderRequest request, ServerCallContext context)
        {
            var orderId = Guid.NewGuid().ToString();
            decimal size = decimal.Parse(request.Size, CultureInfo.InvariantCulture);

            var response = await _b2C2RestClient.OrderAsync(new OrderRequest
            {
                ClientOrderId = orderId,
                Instrument = request.AssetPair,
                Side = size > 0
                    ? Side.Buy
                    : Side.Sell,
                Quantity = Math.Abs(size),
                OrderType = OrderType.MKT,
                ValidUntil = DateTime.UtcNow.AddSeconds(3)
            });

            var result = new ExecuteMarketOrderResponse
            {
                OrderId = response.OrderId,
                RequestId = response.ClientOrderId,
                AssetPairId = request.AssetPair,
                Price = response.ExecutedPrice.ToString(CultureInfo.InvariantCulture),
                FilledSize = (response.Side == Side.Buy ? response.Quantity : -response.Quantity).ToString(CultureInfo.InvariantCulture),
                CancelledSize = "0",
                Timestamp = response.Created.ToTimestamp(),
                Error = ErrorCore.Ok
            };

            result.Trades.AddRange(MapTrades(response.Trades));

            return result;
        }

        private List<Trade> MapTrades(IReadOnlyCollection<B2c2Client.Models.Rest.Trade> trades)
        {
            return trades.Select(x => new Trade
            {
                Id = x.TradeId,
                Price = x.Price.ToString(CultureInfo.InvariantCulture),
                FilledSize = (x.Side == Side.Buy ? x.Quantity : -x.Quantity).ToString(CultureInfo.InvariantCulture),
                CancelledSize = "0",
                Timestamp = x.Created.ToTimestamp()
            }).ToList();
        }

        public override async Task<PlaceMarketOrderResponse> PlaceMarketOrder(MarketOrderRequest request, ServerCallContext context)
        {
            var orderId = Guid.NewGuid().ToString();
            decimal size = decimal.Parse(request.Size, CultureInfo.InvariantCulture);

            var response = await _b2C2RestClient.OrderAsync(new OrderRequest
            {
                ClientOrderId = orderId,
                Instrument = request.AssetPair,
                Side = size > 0
                    ? Side.Buy
                    : Side.Sell,
                Quantity = Math.Abs(size),
                OrderType = OrderType.MKT,
                ValidUntil = DateTime.UtcNow.AddSeconds(3)
            });

            return new PlaceMarketOrderResponse
            {
                Error = ErrorCore.Ok,
                RequestId = orderId
            };
        }

        private List<AssetBalance> Map(IReadOnlyDictionary<string, decimal> balances)
        {
            return balances.Select(x => new AssetBalance
            {
                Asset = x.Key,
                Free = x.Value.ToString(CultureInfo.InvariantCulture),
                Used = "0"
            }).ToList();
        }
    }
}
