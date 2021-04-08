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

        public override async Task<MarketOrderResponse> MarketOrder(MarketOrderRequest request, ServerCallContext context)
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

            return new MarketOrderResponse
            {
                Id = response?.OrderId,
                Executed = response != null
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
