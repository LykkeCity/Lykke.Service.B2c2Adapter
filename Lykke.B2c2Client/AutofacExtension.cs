using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.B2c2Client.Settings;

namespace Lykke.B2c2Client
{
    /// <summary>
    /// Autofac extension for client registration.
    /// </summary>
    public static class AutofacExtension
    {
        /// <summary>
        /// Registers <see cref="IB2С2RestClient"/> in Autofac container using <see cref="B2C2ClientSettings"/>.
        /// </summary>
        /// <param name="builder">Autofac container builder.</param>
        /// <param name="settings">MarketMakerArbitrageDetector client settings.</param>
        public static void RegisterB2С2RestClient(
            [NotNull] this ContainerBuilder builder,
            [NotNull] B2C2ClientSettings settings)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            builder.RegisterType<B2С2RestClient>()
                .As<IB2С2RestClient>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings));
        }

        /// <summary>
        /// Registers <see cref="IB2С2WebSocketClient"/> in Autofac container using <see cref="B2C2ClientSettings"/>.
        /// </summary>
        /// <param name="builder">Autofac container builder.</param>
        /// <param name="settings">MarketMakerArbitrageDetector client settings.</param>
        /// <param name="forceReconnectionInterval">Force reconnection interval.</param>
        public static void RegisterB2С2WebSocketClient(
            [NotNull] this ContainerBuilder builder,
            [NotNull] B2C2ClientSettings settings,
            TimeSpan forceReconnectionInterval)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            builder.RegisterType<B2С2WebSocketClient>()
                .As<IB2С2WebSocketClient>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(forceReconnectionInterval))
                .WithParameter(TypedParameter.From(settings));
        }
    }
}
