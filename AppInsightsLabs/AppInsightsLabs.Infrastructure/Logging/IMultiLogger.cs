using System;
using System.Collections.Generic;

namespace AppInsightsLabs.Infrastructure.Logging
{
    public interface IMultiLogger
    {
        void Info(string text, ILoggerProperties properties = null);
        void Debug(string text, ILoggerProperties properties = null);

        void Warn(string text, ILoggerProperties properties = null);
        void Warn(string text, Exception ex, ILoggerProperties properties = null);

        void Error(string text, ILoggerProperties properties = null);
        void Error(string text, Exception ex, ILoggerProperties properties = null);

        void Fatal(string text, ILoggerProperties properties = null);
        void Fatal(string text, Exception ex, ILoggerProperties properties = null);
    }

    /// <summary>
    /// A proxy to using several ILoggers (setup with IoC) and call their respective methods in a loop, in every method here.
    /// </summary>
    public class MultiLogger : IMultiLogger
    {
        private readonly List<ILogger> _loggers;

        public MultiLogger(List<ILogger> loggers)
        {
            _loggers = loggers;
        }

        public void Info(string text, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Info(text, properties));
        }

        public void Debug(string text, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Debug(text, properties));
        }

        public void Warn(string text, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Warn(text, properties));
        }

        public void Warn(string text, Exception ex, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Warn(text, ex, properties));
        }

        public void Error(string text, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Error(text, properties));
        }

        public void Error(string text, Exception ex, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Error(text, ex, properties));
        }

        public void Fatal(string text, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Fatal(text, properties));
        }

        public void Fatal(string text, Exception ex, ILoggerProperties properties = null)
        {
            _loggers.ForEach(p => p.Fatal(text, ex, properties));
        }
    }
}
