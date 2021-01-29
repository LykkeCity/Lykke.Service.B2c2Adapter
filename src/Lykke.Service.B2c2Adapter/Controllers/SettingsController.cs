using System.Net;
using Lykke.Service.B2c2Adapter.Services;
using Lykke.Service.B2c2Adapter.Settings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Lykke.Service.B2c2Adapter.Controllers
{
    [Route("/api/[controller]")]
    public sealed class SettingsController
    {
        private readonly OrderBooksService _orderBooksService;

        public SettingsController(OrderBooksService orderBooksService)
        {
            _orderBooksService = orderBooksService;
        }

        [SwaggerOperation("GetSettings")]
        [HttpGet]
        [ProducesResponseType(typeof(OrderBooksServiceSettings), (int)HttpStatusCode.OK)]
        public OrderBooksServiceSettings Get()
        {
            return _orderBooksService.GetSettings();
        }
    }
}
