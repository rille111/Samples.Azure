using System;
using MassTransit;

namespace MassTransitRetryPolicy.MassTransitHelpers
{
    public static class BusAddressHelpers
    {
        /// <summary>
        /// Send endpoints for Commands
        /// </summary>
        /// <typeparam name="TCommand">Class name must end in 'Command'</typeparam>
        /// <param name="busAddress">The bus address</param>
        /// <returns>A complete URI to the bus and queue to deliver on.</returns>
        public static Uri CreateSendEndPointUri<TCommand>(Uri busAddress) where TCommand : class, new()
        {
            var className = typeof(TCommand).Name;
            var newConn = busAddress.ExtractBusAddress();

            var newUri = new Uri(newConn + "/" + className);
            return newUri;
        }

        public static string ExtractBusAddress(this IBusControl bus)
        {
            return bus.Address.ExtractBusAddress();
        }

        /// <summary>
        /// Respects rabbitmq "virtual hosts" and azure "paths". These are basically same thing with different name.
        /// </summary>
        public static string ExtractBusAddress(this Uri address)
        {
            var newConn = $"{address.Scheme}://{address.Host}";
            if (address.Scheme == "rabbitmq" && address.Segments.Length > 1)
                newConn += address.Segments[0] + address.Segments[1].TrimEnd('/');
            return newConn;
        }
    }
}
