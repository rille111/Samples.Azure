using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure
{
    public class AppInsightsItemParser
    {
        public IEnumerable<AppInsightsTraceItem> ParseTraceItems(IEnumerable<string> jsonStrings)
        {
            var jsonStringList = jsonStrings.ToList();
            return jsonStringList.Select(AppInsightsTraceItem.Create)
                .Where(p => p != null)
                .OrderBy(p => p.TimeStampUtc);
        }

        public IEnumerable<AppInsightsEventItem> ParseEventItems(IEnumerable<string> jsonStrings)
        {
            var jsonStringList = jsonStrings.ToList();
            return jsonStringList.Select(AppInsightsEventItem.Create)
                .Where(p => p != null)
                .OrderBy(p => p.TimeStampUtc);
        }

        public IEnumerable<AppInsightsExceptionItem> ParseExceptionItems(IEnumerable<string> jsonStrings)
        {
            var jsonStringList = jsonStrings.ToList();
            return jsonStringList.Select(AppInsightsExceptionItem.Create)
                .Where(p => p != null)
                .OrderBy(p => p.TimeStampUtc);
        }

        private bool IsTrace(JObject jObject)
        {
            var hasMessage = jObject["message"] != null;
            return hasMessage;
        }

        private bool IsException(JObject jObject)
        {
            var hasMessage = jObject["basicException"] != null;
            return hasMessage;
        }

        private bool IsEvent(JObject jObject)
        {
            var hasMessage = jObject["event"] != null;
            return hasMessage;
        }
    }
}