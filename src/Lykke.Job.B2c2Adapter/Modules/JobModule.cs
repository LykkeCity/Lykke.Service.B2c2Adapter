using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common;
using Common.Log;
using Lykke.Job.B2c2Adapter.Services;
using Lykke.Job.B2c2Adapter.Settings.JobSettings;
using Lykke.Sdk;
using Lykke.Sdk.Health;
using Lykke.Service.B2c2Adapter.Domain.Services;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.B2c2Adapter.Modules
{
    public class JobModule : Module
    {
        private readonly B2c2AdapterJobSettings _settings;
        private readonly IReloadingManager<B2c2AdapterJobSettings> _settingsManager;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(B2c2AdapterJobSettings settings, IReloadingManager<B2c2AdapterJobSettings> settingsManager)
        {
            _settings = settings;
            _settingsManager = settingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            // NOTE: Do not register entire settings in container, pass necessary settings to services which requires them
            // ex:
            // builder.RegisterType<QuotesPublisher>()
            //  .As<IQuotesPublisher>()
            //  .WithParameter(TypedParameter.From(_settings.Rabbit.ConnectionString))

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();

            // TODO: Add your dependencies here

            builder.Populate(_services);
        }
    }
}
