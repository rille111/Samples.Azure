namespace MassTransitRetryPolicy.MassTransitHelpers
{
    public class BusConfiguration
    {
        /// <summary>
        /// Example: rabbitmq://rabbit-dev.adnet.adlibris.se/[VirtualHost]
        /// Example: sb://adlibris-dev.servicebus.windows.net
        /// AzureSb seems to use "Path", how does that work? Dunno.
        /// Now, how should we see VirtualHost for MassTransit? Is it the Domain for the system?
        /// Networks are segregated by vhosts (http://masstransit.readthedocs.io/en/master/configuration/transports/rabbitmq.html)
        /// </summary>
        public string ConnectionUri { get; set; }

        /// <summary>
        /// Either the login for Rabbit, or policyname (Shared Access Policy) in Azure.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Either the password for Rabbit, or the Shared Access Policy Key in Azure.
        /// </summary>
        public string Password { get; set; }

        
    }
}