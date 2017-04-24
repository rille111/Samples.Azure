using System;

namespace AppInsightsLabs.Infrastructure.Logging
{
    public interface ILogger
    {
        void Debug(string text, ILoggerProperties properties = null);

        void Info(string text, ILoggerProperties properties = null);

        void Warn(string text, ILoggerProperties properties = null);
        void Warn(string text, Exception ex, ILoggerProperties properties = null);

        void Error(string text, ILoggerProperties properties = null);
        void Error(string text, Exception ex, ILoggerProperties properties = null);

        void Fatal(string text, ILoggerProperties properties = null);
        void Fatal(string text, Exception ex, ILoggerProperties properties = null);
    }
}