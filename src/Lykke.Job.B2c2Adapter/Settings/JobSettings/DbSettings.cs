using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.B2c2Adapter.Settings.JobSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
