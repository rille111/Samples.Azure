using System.Collections.Generic;

namespace AppInsightsLabs.Infrastructure.Logging
{
    // ReSharper disable once InconsistentNaming
    public static class ILoggerExtensions
    {
        public static Dictionary<string, string> ToDictionary(this ILoggerProperties properties)
        {
            if (properties == null)
                return null;

            var newDict = new Dictionary<string, string>();
            var typen = properties.GetType();

            foreach (var propInfo in typen.GetProperties())
            {
                var propValue = propInfo.GetValue(properties, null);
                var dictionaryValue = propValue?.ToString() ?? null;

                newDict.Add(propInfo.Name, dictionaryValue);
            }

            return newDict;
        }
    }
}