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

        public string OuterMessageTruncated => OuterMessage?.PadRight(30).Substring(0, 30);

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
            var arr = o["basicException"] as JArray;
            ret.ExceptionType = FindPropertyValueInArray(arr, "exceptionType");
            ret.ProblemId = FindPropertyValueInArray(arr, "problemId");
            ret.OuterMessage = FindPropertyValueInArray(arr, "outerMessage") ??
                               FindPropertyValueInArray(arr, "outerExceptionMessage") ??
                               FindPropertyValueInArray(arr, "message");

            // Custom dimensions
            var dims = o["context"]["custom"]["dimensions"] as JArray;
            ret.AppName = FindPropertyValueInArray(dims, "application_Name");
            ret.AppContext = FindPropertyValueInArray(dims, "application_LogContext");
            ret.AppVersion = FindPropertyValueInArray(dims, "application_Version");
            return ret;
        }
    }
}