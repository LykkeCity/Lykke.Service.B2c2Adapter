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
        private readonly TradeHistoryService _tradeHistoryService;
        private readonly LedgerHistoryService _ledgerHistoryService;

        public ReportController(TradeHistoryService tradeHistoryService, LedgerHistoryService ledgerHistoryService)
        {
            _tradeHistoryService = tradeHistoryService;
            _ledgerHistoryService = ledgerHistoryService;
        }

        [SwaggerOperation("ReloadTradeHistory")]
        [HttpPost("reloadtrades")]
        [ProducesResponseType(typeof(int), (int) HttpStatusCode.OK)]
        public Task<int> ReloadTradeHistory()
        {
            return _tradeHistoryService.ReloadTradeHistoryAsync();
        }

        [SwaggerOperation("ReloadLedgerHistory")]
        [HttpPost("reloadLedgers")]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public Task<int> ReloadLedgerHistory()
        {
            return _ledgerHistoryService.ReloadLedgerHistoryAsync();
        }
    }
}
