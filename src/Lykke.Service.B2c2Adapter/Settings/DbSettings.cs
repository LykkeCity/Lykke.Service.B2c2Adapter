using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.B2c2Adapter.Settings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        public string ReportSqlConnString { get; set; }
    }
}
