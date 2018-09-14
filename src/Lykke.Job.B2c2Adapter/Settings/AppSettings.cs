using Lykke.Job.B2c2Adapter.Settings.JobSettings;
using Lykke.Job.B2c2Adapter.Settings.SlackNotifications;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.B2c2Adapter.Settings
{
    public class AppSettings
    {
        public B2c2AdapterJobSettings B2c2AdapterJob { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        [Optional]
        public MonitoringServiceClientSettings MonitoringServiceClient { get; set; }
    }
}
