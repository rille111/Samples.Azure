using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure
{
    public class AppInsightsTraceParser
    {
        public IEnumerable<TraceItem> ParseFromStrings(IEnumerable<string> jsonStrings)
        {
            var traces = jsonStrings
                .Select(ParseFromString)
                .Where(p => p != null)
                .OrderBy(p => p.TimeStampUtc);

            return traces;
        }

        public TraceItem ParseFromString(string jsonString)
        {
            var o = JObject.Parse(jsonString);

            if (!IsTrace(o))
                return null;

            var itm = new TraceItem();
            itm.SeverityLevel = (string)o["message"][0]["severityLevel"];
            itm.MessageRaw = (string)o["message"][0]["raw"];
            itm.TimeStampUtc = DateTime.Parse((string) o["context"]["data"]["eventTime"]);
            itm.RoleInstance = (string)o["context"]["device"]["roleInstance"];
            itm.Id = (Guid) o["internal"]["data"]["id"];

            var customDims = o["context"]?["custom"]?["dimensions"] as JArray; // array

            if (customDims != null)
            {
                foreach (var jToken in customDims.Children())
                {
                    var jProp = (JProperty)jToken.First();
                    itm.CustomDimensions.Add(jProp.Name, jProp.Value.ToString());
                }
            }

            return itm;
        }

        private bool IsTrace(JObject jObject)
        {
            var hasMessage = jObject["message"] != null;
            return hasMessage;
        }
    }
}