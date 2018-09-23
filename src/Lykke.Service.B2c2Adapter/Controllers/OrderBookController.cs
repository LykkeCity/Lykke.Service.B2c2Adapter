using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.SpotController;
using Lykke.Common.Log;
using Lykke.Service.B2c2Adapter.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.B2c2Adapter.Controllers
{
    public sealed class OrderBookController : IOrderBookController
    {
        private readonly B2c2OrderBooksService _b2C2OrderBooksService;

        public OrderBookController(B2c2OrderBooksService b2C2OrderBooksService, ILogFactory logFactory)
        {
            _b2C2OrderBooksService = b2C2OrderBooksService;
        }

        [SwaggerOperation("GetAllInstruments")]
        [HttpGet("GetAllInstruments")]
        public IReadOnlyCollection<string> GetAllInstruments()
        {
            return _b2C2OrderBooksService.GetAllInstruments();
        }

        [SwaggerOperation("GetAllTickPrices")]
        [HttpGet("GetAllTickPrices")]
        public async Task<IReadOnlyCollection<TickPrice>> GetAllTickPrices()
        {
            return await _b2C2OrderBooksService.GetAllTickPrices();
        }

        [SwaggerOperation("GetOrderBook")]
        [HttpGet("GetOrderBook")]
        public async Task<OrderBook> GetOrderBook(string assetPair)
        {
            return await _b2C2OrderBooksService.GetOrderBook(assetPair);
        }
    }
}
