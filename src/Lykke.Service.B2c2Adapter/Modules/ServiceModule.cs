using Autofac;
using Common;
using Lykke.B2c2Client;
using Lykke.Service.B2c2Adapter.RabbitPublishers;
using Lykke.Service.B2c2Adapter.Services;
using Lykke.Service.B2c2Adapter.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.B2c2Adapter.Modules
{
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
            // B2C2 Client Lybrary
            builder.RegisterType<B2c2RestClient>()
                .As<IB2c2RestClient>()
                .SingleInstance()
                .WithParameter("url", _settings.RestUrl)
                .WithParameter("authorizationToken", _settings.AuthorizationToken);
            builder.RegisterType<B2c2WebSocketClient>()
                .As<IB2c2WebSocketClient>()
                .SingleInstance()
                .WithParameter("url", _settings.WebSocketUrl)
                .WithParameter("authorizationToken", _settings.AuthorizationToken);

            // Publishers
            builder.RegisterType<OrderBookPublisher>()
                .As<IOrderBookPublisher>()
                .As<IStartable>()
                .As<IStopable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.RabbitMq.OrderBooks));
            builder.RegisterType<TickPricePublisher>()
                .As<ITickPricePublisher>()
                .As<IStartable>()
                .As<IStopable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.RabbitMq.TickPrices));

            // B2C2 order books service
            builder.RegisterType<B2c2OrderBooksService>()
                .As<IStartable>()
                .As<IStopable>()
                .SingleInstance()
                .WithParameter("instrumentsLevels", _settings.InstrumentLevels);
        }
    }
}
