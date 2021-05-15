using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Lykke.Service.B2c2Adapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class B2c2AdapterSettings
    {
        public string RestUrl { get; set; }

        public string WebSocketUrl { get; set; }

        public string AuthorizationToken { get; set; }

        // from, to
        public IReadOnlyDictionary<string, string> AssetMappings { get; set; } = new Dictionary<string, string>();

        public IReadOnlyList<InstrumentLevels> InstrumentLevels { get; set; } = new List<InstrumentLevels>();

        public TimeSpan ReconnectIfNeededInterval { get; set; }

        public TimeSpan ForceReconnectInterval { get; set; }

        public DbSettings Db { get; set; }

        public bool EnableExportToReportDb { get; set; }

        public RabbitMqSettings RabbitMq { get; set; }

        public string VenueName { get; set; }
    }
}
