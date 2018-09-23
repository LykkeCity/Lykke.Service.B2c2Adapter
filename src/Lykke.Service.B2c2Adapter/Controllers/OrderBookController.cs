using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.SpotController;
using Lykke.Service.B2c2Adapter.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.B2c2Adapter.Controllers
{
    [Route("/api/[controller]")]
    public sealed class OrderBookController : IOrderBookController
    {
        private readonly B2c2OrderBooksService _b2C2OrderBooksService;

        public OrderBookController(B2c2OrderBooksService b2C2OrderBooksService)
        {
            _b2C2OrderBooksService = b2C2OrderBooksService;
        }

        [SwaggerOperation("GetAllInstruments")]
        [HttpGet("GetAllInstruments")]
        [ProducesResponseType(typeof(IReadOnlyCollection<string>), (int)HttpStatusCode.OK)]
        public IReadOnlyCollection<string> GetAllInstruments()
        {
            return _b2C2OrderBooksService.GetAllInstruments();
        }

        [SwaggerOperation("GetAllTickPrices")]
        [HttpGet("GetAllTickPrices")]
        [ProducesResponseType(typeof(IReadOnlyCollection<TickPrice>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyCollection<TickPrice>> GetAllTickPrices()
        {
            return _b2C2OrderBooksService.GetAllTickPrices();
        }

        [SwaggerOperation("GetOrderBook")]
        [HttpGet("GetOrderBook")]
        [ProducesResponseType(typeof(OrderBook), (int)HttpStatusCode.OK)]
        public async Task<OrderBook> GetOrderBook(string assetPair)
        {
            if (string.IsNullOrWhiteSpace(assetPair))
                return null;

            var result = _b2C2OrderBooksService.GetOrderBook(assetPair.ToUpper());

            return result;
        }
    }
}
