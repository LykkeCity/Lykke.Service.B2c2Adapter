using Prometheus;

namespace Lykke.Service.B2c2Adapter.Utils
{
    public static class InternalMetrics
    {
        public static readonly Counter OrderBookInCount = Metrics
            .CreateCounter("order_book_in_count",
                "Counter of received order books.",
                new CounterConfiguration { LabelNames = new[] { "symbol" } });

        public static readonly Gauge OrderBookInDelayMilliseconds = Metrics
            .CreateGauge("order_book_in_delay_ms",
                "Gauge of order books delay between original timestamp and when we received in milliseconds.",
                new GaugeConfiguration { LabelNames = new[] { "symbol" } });

        public static readonly Counter OrderBookOutCount = Metrics
            .CreateCounter("order_book_out_count",
                "Counter of sent order books.",
                new CounterConfiguration {LabelNames = new[] {"symbol"}});

        public static readonly Gauge OrderBookOutDelayMilliseconds = Metrics
            .CreateGauge("order_book_out_delay_ms",
                "Gauge of order books delay between receiving and sent in milliseconds.",
                new GaugeConfiguration {LabelNames = new[] {"symbol"}});

        public static readonly Counter QuoteOutCount = Metrics
            .CreateCounter("quote_out_count",
                "Counter of sent quotes.",
                new CounterConfiguration {LabelNames = new[] {"symbol"}});

        public static readonly Gauge QuoteOutSidePrice = Metrics
            .CreateGauge("quote_out_side_price",
                "Gauge of received quote side price.",
                new GaugeConfiguration {LabelNames = new[] {"symbol", "side"}});
        
        public static readonly Gauge OrderBookOutDictionarySize = Metrics
            .CreateGauge("order_book_out_dictionary_size", "Gauge of order books dictionary size.");
        
        public static readonly Counter MessagesOutCount = Metrics
            .CreateCounter("messages_out_count",
                "Counter of sent messages.",
                new CounterConfiguration());
        
        public static readonly Gauge OutBatchSize = Metrics
            .CreateGauge("out_batch_size", "Gauge of order books in a batch.");
        
        public static readonly Gauge OrderBookOutSidePrice = Metrics
            .CreateGauge("order_book_out_side_price",
                "Gauge of published order book side price.",
                new GaugeConfiguration { LabelNames = new[] { "exchange", "symbol", "side" } });
    }
}
