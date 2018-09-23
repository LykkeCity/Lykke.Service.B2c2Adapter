using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.B2c2Adapter.Settings
{
    public sealed class PublishingSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
        public bool Enabled { get; set; }
    }
}
