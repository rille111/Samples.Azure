using System;
using MassTransit;

namespace MassTransitRetryPolicy.MassTransitHelpers
{
    public static class BusConfigurationExtensions
    {
        public static void ValidateConfigurationThrows(this BusConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException($"{nameof(configuration)}");
            if (configuration.ConnectionUri == null)
                throw new ArgumentNullException($"{nameof(configuration.ConnectionUri)}");
            if (configuration.Login == null)
                throw new ArgumentNullException($"{nameof(configuration.Login)}");
            if (configuration.Password == null)
                throw new ArgumentNullException($"{nameof(configuration.Password)}");
            if (configuration.ConnectionUri.EndsWith("/"))
                throw new ArgumentException($"ConnectionUri can not end with slash!", $"{nameof(configuration.ConnectionUri)}");
        }

        /// <summary>
        /// Prefer using this method when configuring a send endpoint, so that the consumers for a command/event will listen on the correct queue.
        /// </summary>
        /// <typeparam name="TMessage">Must end with either Command or Event (convention)</typeparam>
        public static void ReceiveEndpoint<TMessage>(this IBusFactoryConfigurator cfg, Action<IReceiveEndpointConfigurator> conf)
        {
            var className = typeof(TMessage).Name;
            cfg.ReceiveEndpoint(className, conf);
        }
    }
}