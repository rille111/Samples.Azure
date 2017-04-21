using System;

namespace AppInsightsLabs.Infrastructure.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Debug(string text, ILoggerProperties properties = null)
        {
            Console.Out.WriteLine(text);
        }

        public void Info(string text, ILoggerProperties properties = null)
        {
            Console.Out.WriteLine(text);
        }

        public void Warn(string text, ILoggerProperties properties = null)
        {
            Console.Out.WriteLine(text);
        }

        public void Warn(string text, Exception ex, ILoggerProperties properties = null)
        {
            Console.Out.WriteLine($"{text}\n{ex}");
        }

        public void Error(string text, ILoggerProperties properties = null)
        {
            Console.Error.WriteLine(text);
        }

        public void Error(string text, Exception ex, ILoggerProperties properties = null)
        {
            Console.Error.WriteLine($"{text}\n{ex}\nproperties: {properties}");
        }

        public void Fatal(string text, ILoggerProperties properties = null)
        {
            Console.Error.WriteLine(text);
        }

        public void Fatal(string text, Exception ex, ILoggerProperties properties = null)
        {
            Console.Error.WriteLine($"{text}\n{ex}");
        }
    }
}