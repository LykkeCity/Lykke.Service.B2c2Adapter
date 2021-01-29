using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.SpotController;
using Lykke.Service.B2c2Adapter.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.B2c2Adapter.Controllers
{
    [Route("/api/[controller]")]
    public sealed class OrderBookController : IOrderBookController
    {
        private readonly OrderBooksService _orderBooksService;

        public OrderBookController(OrderBooksService orderBooksService)
        {
            _orderBooksService = orderBooksService;
        }

        [SwaggerOperation("GetAllInstruments")]
        [HttpGet("GetAllInstruments")]
        [ProducesResponseType(typeof(IReadOnlyCollection<string>), (int)HttpStatusCode.OK)]
        public IReadOnlyCollection<string> GetAllInstruments()
        {
            return _orderBooksService.GetAllInstruments();
        }

        [SwaggerOperation("GetAllTickPrices")]
        [HttpGet("GetAllTickPrices")]
        [ProducesResponseType(typeof(IReadOnlyCollection<TickPrice>), (int)HttpStatusCode.OK)]
        public Task<IReadOnlyCollection<TickPrice>> GetAllTickPrices()
        {
            return Task.FromResult(_orderBooksService.GetAllTickPrices());
        }

        [SwaggerOperation("GetOrderBook")]
        [HttpGet("GetOrderBook")]
        [ProducesResponseType(typeof(OrderBook), (int)HttpStatusCode.OK)]
        public Task<OrderBook> GetOrderBook(string assetPair)
        {
            if (string.IsNullOrWhiteSpace(assetPair))
                return null;

            var result = _orderBooksService.GetOrderBook(assetPair.ToUpper());

            return Task.FromResult(result);
        }
    }
}
