using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AppInsightsLabs.Infrastructure.Logging
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// http://stackoverflow.com/questions/37551596/application-insights-not-logging-custom-events
    /// http://apmtips.com/blog/2015/02/02/developer-mode/
    /// </remarks>
    public class AppInsightsLogger : ILogger
    {
        private readonly TelemetryClient _telemetryClient;

        public AppInsightsLogger(string instrumentationKey)
        {
            var config = new TelemetryConfiguration {InstrumentationKey = instrumentationKey};
            
            ((InMemoryChannel) config.TelemetryChannel).DeveloperMode = true;
            ((InMemoryChannel) config.TelemetryChannel).MaxTelemetryBufferCapacity = 1;


            _telemetryClient = new TelemetryClient();
            
        }

        public void Debug(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Verbose, properties.ToDictionary());
        }

        public void Info(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Information, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void Warn(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Warning, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void Warn(string text, Exception ex, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace($"{text}\n{ex}", SeverityLevel.Warning, properties.ToDictionary());
            _telemetryClient.TrackException(ex, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void Error(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Error, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void Error(string text, Exception ex, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace($"{text}\n{ex}", SeverityLevel.Error, properties.ToDictionary());
            _telemetryClient.TrackException(ex, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void Fatal(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace(text, SeverityLevel.Critical, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void Fatal(string text, Exception ex, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackTrace($"{text}\n{ex}", SeverityLevel.Critical, properties.ToDictionary());
            _telemetryClient.TrackException(ex, properties.ToDictionary());
            _telemetryClient.Flush();
        }

        public void CustomEvent(string text, ILoggerProperties properties = null)
        {
            _telemetryClient.TrackEvent(text, properties.ToDictionary());
        }
    }
}