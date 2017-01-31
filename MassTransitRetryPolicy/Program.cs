using System;
using GreenPipes;
using MassTransit;
using MassTransitRetryPolicy.MassTransitHelpers;

namespace MassTransitRetryPolicy
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start Bus
            Console.WriteLine("Starting, please wait ..\n");
            var bus = CreateIBusControl();
            bus.Start();
            Console.WriteLine("Bus started, sending commands ..\n");

            // Send Commands
            var commandSendpoint = bus.GetSendEndpointAsync<WorldCommand>().GetAwaiter().GetResult();
            commandSendpoint.Send (new WorldCommand
            {
                Message = "Hello!"
            });
            Console.WriteLine("Commands have been sent\n");


            // Wait and Exit
            Console.WriteLine("Press any key to exit\n");
            Console.ReadKey();
            bus.Stop();
            }

        private static IBusControl CreateIBusControl()
        {
            var config = new BusConfiguration
            {
                ConnectionUri = "sb://adlibris-product-ais-rickard-bus-dev.servicebus.windows.net",
                Login = "RootManageSharedAccessKey",
                Password = "sAWcvN8kPyhR7bUvcwIvfNL0OWeajuHluKkHff52n+M="
            };

            var configurator = new AzureSbBusConfigurator(config);
            var bus = configurator
                .CreateBus((cfg, host) =>
                {
                    // Command Consumers 
                    cfg.ReceiveEndpoint<WorldCommand>(c =>
                    {
                        c.Consumer<WorldCommandConsumer>();
                    });

                    // TODO: below doesn't work
                    //cfg.UseRetry(r => Retry.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5)));
                    // TODO: but this works ..
                    cfg.UseRetry(Retry.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5))); // works
                });
            return bus;
        }
    }
}
