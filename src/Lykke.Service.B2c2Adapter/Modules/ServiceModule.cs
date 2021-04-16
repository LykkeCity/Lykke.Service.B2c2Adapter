using Antares.Sdk.Services;
using Autofac;
using Autofac.Core.NonPublicProperty;
using JetBrains.Annotations;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Settings;
using Lykke.Service.B2c2Adapter.Managers;
using Lykke.Service.B2c2Adapter.RabbitMq.Publishers;
using Lykke.Service.B2c2Adapter.Services;
using Lykke.Service.B2c2Adapter.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.B2c2Adapter.Modules
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;
        private B2c2AdapterSettings _settings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
            _settings = _appSettings.CurrentValue.B2c2AdapterService;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var webSocketSettings = new B2C2ClientSettings(_settings.WebSocketUrl, _settings.AuthorizationToken);

            builder.RegisterType<Manager>()
                .As<IStartupManager>()
                .As<IShutdownManager>()
                .AutoWireNonPublicProperties();

            // B2C2 Client Library
            builder.RegisterB2С2RestClient(new B2C2ClientSettings(_settings.RestUrl, _settings.AuthorizationToken));

            // Publishers
            builder.RegisterType<OrderBookPublisher>()
                .AsSelf()
                .As<IOrderBookPublisher>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.RabbitMq.OrderBooks));

            builder.RegisterType<TickPricePublisher>()
                .AsSelf()
                .As<ITickPricePublisher>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.RabbitMq.TickPrices));

            // Order books service
            var orderBooksServiceSettings = new OrderBooksServiceSettings
            {
                InstrumentsLevels = _settings.InstrumentLevels,
                ReconnectIfNeededInterval = _settings.ReconnectIfNeededInterval,
                ForceReconnectInterval = _settings.ForceReconnectInterval
            };

            builder.RegisterType<OrderBooksService>()
                .AsSelf()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.AssetMappings))
                .WithParameter(TypedParameter.From(orderBooksServiceSettings))
                .WithParameter(TypedParameter.From(webSocketSettings));

            // Reports

            builder.RegisterType<TradeHistoryService>()
                .AsSelf()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.AssetMappings))
                .WithParameter(TypedParameter.From(_settings.Db.ReportSqlConnString))
                .WithParameter(TypedParameter.From(_settings.EnableExportToReportDb));

            builder.RegisterType<BalanceHistoryService>()
                .AsSelf()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.AssetMappings))
                .WithParameter(TypedParameter.From(_settings.Db.ReportSqlConnString))
                .WithParameter(TypedParameter.From(_settings.EnableExportToReportDb));

            builder.RegisterType<LedgerHistoryService>()
                .AsSelf()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.AssetMappings))
                .WithParameter(TypedParameter.From(_settings.Db.ReportSqlConnString))
                .WithParameter(TypedParameter.From(_settings.EnableExportToReportDb));
        }
    }
}
