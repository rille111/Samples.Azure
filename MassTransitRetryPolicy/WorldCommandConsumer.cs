using System;
using System.Threading.Tasks;
using MassTransit;

namespace MassTransitRetryPolicy
{
    public class WorldCommandConsumer : IConsumer<WorldCommand>
    {
        public async Task Consume(ConsumeContext<WorldCommand> context)
        {
            await Console.Out.WriteLineAsync($"{nameof(WorldCommand)} says: {context.Message.Message}");
        }
    }
}
