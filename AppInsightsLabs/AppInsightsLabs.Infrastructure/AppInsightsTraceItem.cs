using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure
{
    public class AppInsightsTraceItem : AppInsightsItem
    {

        public string SeverityLevel { get; set; }
        public string MessageRaw { get; set; }
        public string MessageTruncated => MessageRaw.PadRight(30).Substring(0, 30);

        public override string ToString()
        {
            return $"TRACE > TimeStampUtc: {TimeStampUtc}, SeverityLevel: {SeverityLevel}\n\tMessageTruncated: {MessageTruncated}\n\tCustomDimensions:\n{FlattenCustomDimensions()}";
        }


        public static AppInsightsTraceItem Create(string jsonString)
        {
            var ret = new AppInsightsTraceItem();
            var o = JObject.Parse(jsonString);
            ret.ParseCommon(o);
            ret.SeverityLevel = (string)o["message"][0]["severityLevel"];
            ret.MessageRaw = (string)o["message"][0]["raw"];
            return ret;
        }
    }
}