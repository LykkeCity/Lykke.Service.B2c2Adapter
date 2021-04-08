using System;
using JetBrains.Annotations;
using Lykke.Service.B2c2Adapter.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Antares.Sdk;
using Autofac;
using Lykke.B2c2Client.Settings;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.B2c2Adapter.Grpc;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Prometheus;

namespace Lykke.Service.B2c2Adapter
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "B2c2Adapter API",
            ApiVersion = "v1"
        };

        private IReloadingManagerWithConfiguration<AppSettings> _settings;
        private LykkeServiceOptions<AppSettings> _lykkeOptions;

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            (_lykkeOptions, _settings) = services.ConfigureServices<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "B2c2AdapterLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.B2c2AdapterService.Db.LogsConnString;
                };
            });

            services.AddHttpClient(ClientNames.B2C2ClientName, client =>
            {
                client.BaseAddress = new Uri(_settings.CurrentValue.B2c2AdapterService.RestUrl);
                client.DefaultRequestHeaders.Add("Authorization",
                    $"Token {_settings.CurrentValue.B2c2AdapterService.AuthorizationToken}");
            });

            services.AddGrpc();
            services.AddGrpcReflection();
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
                options.DefaultErrorHandler = exception => ErrorResponse.Create(exception.Message);
            });

            app.UseMetricServer();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcReflectionService();
                endpoints.MapGrpcService<PrivateService>();
            });
        }

        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var configurationRoot = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder.ConfigureContainerBuilder(_lykkeOptions, configurationRoot, _settings);
        }
    }
}
