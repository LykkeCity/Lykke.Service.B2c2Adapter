using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.B2c2Adapter.Settings
{
    public class MonitoringServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string MonitoringServiceUrl { get; set; }
    }
}
