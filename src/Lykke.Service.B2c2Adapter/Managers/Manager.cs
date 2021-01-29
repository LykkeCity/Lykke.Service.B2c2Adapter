﻿using System.Threading.Tasks;
using Antares.Sdk.Services;
using Lykke.Service.B2c2Adapter.RabbitMq.Publishers;
using Lykke.Service.B2c2Adapter.Services;

namespace Lykke.Service.B2c2Adapter.Managers
{
    public class Manager : IStartupManager, IShutdownManager
    {
        private OrderBookPublisher OrderBookPublisher { get; set; }
        private TickPricePublisher TickPricePublisher { get; set; }
        private OrderBooksService OrderBooksService { get; set; }
        private TradeHistoryService TradeHistoryService { get; set; }
        private BalanceHistoryService BalanceHistoryService { get; set; }
        private LedgerHistoryService LedgerHistoryService { get; set; }

        public Task StartAsync()
        {
            OrderBookPublisher.Start();

            TickPricePublisher.Start();

            OrderBooksService.Start();

            TradeHistoryService.Start();

            BalanceHistoryService.Start();

            LedgerHistoryService.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            OrderBookPublisher.Stop();

            TickPricePublisher.Stop();

            OrderBooksService.Stop();

            TradeHistoryService.Stop();

            BalanceHistoryService.Stop();

            LedgerHistoryService.Stop();

            return Task.CompletedTask;
        }
    }
}
