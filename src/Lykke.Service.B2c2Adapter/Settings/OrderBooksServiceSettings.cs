using System;
using System.Collections.Generic;

namespace Lykke.Service.B2c2Adapter.Settings
{
    public class OrderBooksServiceSettings
    {
        public IReadOnlyList<InstrumentLevels> InstrumentsLevels { get; set; } = new List<InstrumentLevels>();

        public TimeSpan ReconnectIfNeededInterval { get; set; }

        public TimeSpan PublishFromCacheInterval { get; set; }

        public TimeSpan ForceReconnectInterval { get; set; }
    }
}
