﻿namespace Lykke.Service.B2c2Adapter.Settings
{
    public sealed class RabbitMqSettings
    {
        public PublishingSettings OrderBooks { get; set; }
        public PublishingSettings TickPrices { get; set; }
    }
}
