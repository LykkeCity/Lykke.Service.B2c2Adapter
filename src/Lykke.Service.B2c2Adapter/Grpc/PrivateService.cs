﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Lykke.Service.B2c2Adapter.Settings;
using Microsoft.Extensions.Logging;
using Swisschain.Liquidity.ApiContract;
using ErrorCode = Swisschain.Liquidity.ApiContract.ErrorCode;
using Trade = Swisschain.Liquidity.ApiContract.Trade;

namespace Lykke.Service.B2c2Adapter.Grpc
{
    public class PrivateService : PrivateGrpc.PrivateGrpcBase
    {
        private readonly B2c2AdapterSettings _settings;
        private readonly IB2С2RestClient _b2C2RestClient;
        private readonly ILogger<PrivateService> _logger;

        public PrivateService(
            B2c2AdapterSettings settings,
            IB2С2RestClient b2C2RestClient,
            ILogger<PrivateService> logger
            )
        {
            _settings = settings;
            _b2C2RestClient = b2C2RestClient;
            _logger = logger;
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
            if (!_settings.InstrumentMappings.TryGetValue(request.AssetPair, out var assetPairId))
            {
                _logger.LogWarning("Asset pair not found. {assetPairId}", request.AssetPair);

                return new ExecuteMarketOrderResponse
                {
                    Error = ErrorCode.Critical,
                    ErrorMessage = "Asset pair not found"
                };
            }

            var orderId = $"{((int)request.TradeTypeTag).ToString()}{request.ComponentTag}{Guid.NewGuid().ToString().Remove(0, 1 + request.ComponentTag.Length)}";
            decimal size = decimal.Parse(request.Size, CultureInfo.InvariantCulture);

            var response = await _b2C2RestClient.OrderAsync(new OrderRequest
            {
                ClientOrderId = orderId,
                Instrument = assetPairId,
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
                Error = ErrorCode.Ok
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
            if (!_settings.InstrumentMappings.TryGetValue(request.AssetPair, out var assetPairId))
            {
                _logger.LogWarning("Asset pair not found. {assetPairId}", request.AssetPair);

                return new PlaceMarketOrderResponse
                {
                    Error = ErrorCode.Critical,
                    ErrorMessage = "Asset pair not found"
                };

            }
            var orderId = $"{((int)request.TradeTypeTag).ToString()}{request.ComponentTag}{Guid.NewGuid().ToString().Remove(0, 1 + request.ComponentTag.Length)}";
            decimal size = decimal.Parse(request.Size, CultureInfo.InvariantCulture);

            var response = await _b2C2RestClient.OrderAsync(new OrderRequest
            {
                ClientOrderId = orderId,
                Instrument = assetPairId,
                Side = size > 0
                    ? Side.Buy
                    : Side.Sell,
                Quantity = Math.Abs(size),
                OrderType = OrderType.MKT,
                ValidUntil = DateTime.UtcNow.AddSeconds(3)
            });

            return new PlaceMarketOrderResponse
            {
                Error = ErrorCode.Ok,
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
