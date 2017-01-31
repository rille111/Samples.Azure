using System;
using MassTransit;
using MassTransit.AzureServiceBusTransport;
using Microsoft.ServiceBus;

namespace MassTransitRetryPolicy.MassTransitHelpers
{
    /// <summary>
    ///  Helps configure buses, using a Configuration class.
    /// </summary>
    public class AzureSbBusConfigurator
    {
        public BusConfiguration Configuration { get; set; }

        public AzureSbBusConfigurator(BusConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IBusControl CreateBus(Action<IBusFactoryConfigurator, IHost> registrationAction = null)
        {
            var buz = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                var connUri = new Uri(Configuration.ConnectionUri);
                var host = cfg.Host(connUri, hst =>
                {
                    hst.TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(Configuration.Login,
                        Configuration.Password);
                });

                cfg.UseJsonSerializer();
                registrationAction?.Invoke(cfg, host);
            });
            return buz;
        }
    }
}