using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AppInsightsLabs.Infrastructure.Logging
{
    public class AppInsightsLogger : ILogger
    {
        private readonly TelemetryClient _telemetryClient;

        public AppInsightsLogger(string instrumentationKey)
        {
            _telemetryClient = new TelemetryClient(new TelemetryConfiguration { InstrumentationKey = instrumentationKey });
        }

        public void Debug(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Verbose, properties.ToDictionary());
        }

        public void Info(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Information, properties.ToDictionary());
        }

        public void Warn(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Warning, properties.ToDictionary());
        }

        public void Warn(string text, Exception ex, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace($"{text}\n{ex}", SeverityLevel.Warning, properties.ToDictionary());
            _telemetryClient.TrackException(ex, properties.ToDictionary());
        }

        public void Error(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Error, properties.ToDictionary());
        }

        public void Error(string text, Exception ex, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace($"{text}\n{ex}", SeverityLevel.Error, properties.ToDictionary());
            _telemetryClient.TrackException(ex, properties.ToDictionary());
        }

        public void Fatal(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Critical, properties.ToDictionary());
        }

        public void Fatal(string text, Exception ex, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace($"{text}\n{ex}", SeverityLevel.Critical, properties.ToDictionary());
            _telemetryClient.TrackException(ex, properties.ToDictionary());
        }
    }
}