using System.Net;
using System.Threading.Tasks;
using Lykke.Service.B2c2Adapter.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.B2c2Adapter.Controllers
{
    [Route("/api/[controller]")]
    public sealed class ReportController
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [SwaggerOperation("ReloadTradeHistory")]
        [HttpPost("reloadtrades")]
        [ProducesResponseType(typeof(int), (int) HttpStatusCode.OK)]
        public Task<int> ReloadTradeHistory()
        {
            return _reportService.ReloadTradeHistoryAsync();
        }
    }
}
