namespace AppInsightsLabs.Infrastructure.Logging
{
    /// <summary>
    /// Marker interface. Derive from this and use your own logging-properties class.
    /// That class will get it's properties translated and saved in for example AppInsights as their own fields.
    /// </summary>
    public interface ILoggerProperties { }
}