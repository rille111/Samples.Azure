using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
{
    public class AppInsightsExceptionItem : AppInsightsItem
    {
        public string ExceptionType { get; set; }
        public string ProblemId { get; set; }
        public string OuterMessage { get; set; }
        public string AppName { get; set; }
        public string AppVersion { get; set; }
        public string AppContext { get; set; }

        public string OuterMessageTruncated => OuterMessage.PadRight(30).Substring(0, 30);

        public override string ToString()
        {
            return $"EXCEPTION > TimeStampUtc: {TimeStampUtc}, ExceptionType: {ExceptionType}, OuterMessageTruncated: {OuterMessageTruncated}\n\tCustomDimensions:\n{FlattenCustomDimensions()}";
        }

        public static AppInsightsExceptionItem Create(string jsonString)
        {
            var ret = new AppInsightsExceptionItem();
            var o = JObject.Parse(jsonString);
            ret.ParseCommon(o);

            // Common
            ret.ExceptionType = (string)o["basicException"][0]?["exceptionType"];
            ret.ProblemId = (string)o["basicException"][0]?["problemId"];
            ret.OuterMessage = (string)o["basicException"][1]?["outerExceptionMessage"];

            // Custom dimensions
            var cust = o["context"]["custom"]["dimensions"] as JArray;
            if (cust == null || cust.Count == 0)
                return ret;

            ret.AppName = FindCustomDimensionsProperty(cust, "application_Name");
            ret.AppContext = FindCustomDimensionsProperty(cust, "application_LogContext");
            ret.AppVersion = FindCustomDimensionsProperty(cust, "application_Version");
            return ret;
        }


    }
}