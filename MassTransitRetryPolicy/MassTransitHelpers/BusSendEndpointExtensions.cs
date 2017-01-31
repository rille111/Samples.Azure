using System;
using System.Threading.Tasks;
using MassTransitRetryPolicy.MassTransitHelpers;

// ReSharper disable once CheckNamespace
namespace MassTransit
{
    public static class BusSendEndpointExtensions
    {
        /// <summary>
        /// Uses the class name of TCommand to determine the name of the queue (Queue name should be unique per any Command)
        /// TCommand MUST end with 'Command'
        /// </summary>
        public static async Task<ISendEndpoint> GetSendEndpointAsync<TCommand>(this IBusControl bus) where TCommand : class, new()
        {
            var commandClassName = typeof(TCommand).Name;
            if (!commandClassName.EndsWith("Command"))
                throw new InvalidCastException($"Bus Send Endpoints should only be based on commands (class must be suffixed with `Command). Events have no send endpoints. You tried to use: {commandClassName}. Case Sensitive!");

            return await GetSendEndpointAsync(bus, commandClassName);
        }

        private static async Task<ISendEndpoint> GetSendEndpointAsync(this IBusControl bus, string deliverOnQueue)
        {
            var newConn = $"{bus.Address.Scheme}://{bus.Address.Host}";
            if (bus.Address.Scheme == "rabbitmq" && bus.Address.Segments.Length > 1)
                newConn += bus.Address.Segments[0] + bus.Address.Segments[1].TrimEnd('/');

            if (deliverOnQueue == null)
                throw new ArgumentNullException($"{nameof(deliverOnQueue)}");
            var newUri = new Uri(newConn + "/" + deliverOnQueue);
            var sender = await bus.GetSendEndpoint(newUri);

            return sender;
        }

    }
}
