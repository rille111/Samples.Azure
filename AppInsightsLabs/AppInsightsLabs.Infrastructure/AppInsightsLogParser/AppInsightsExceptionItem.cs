using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
{
    public class AppInsightsExceptionItem : AppInsightsItem
    {

        public string ExceptionType { get; set; }

        public string Method { get; set; }

        public string Message { get; set; }


        public string MessageTruncated => Message.PadRight(30).Substring(0, 30);

        public override string ToString()
        {
            return $"EXCEPTION > TimeStampUtc: {TimeStampUtc}, ExceptionType: {ExceptionType}, MessageTruncated: {MessageTruncated}\n\tCustomDimensions:\n{FlattenCustomDimensions()}";
        }

        public static AppInsightsExceptionItem Create(string jsonString)
        {
            var ret = new AppInsightsExceptionItem();
            var o = JObject.Parse(jsonString);
            ret.ParseCommon(o);

            ret.ExceptionType = (string)o["basicException"][0]?["exceptionType"];
            ret.Method = (string)o["basicException"][0]?["method"];
            ret.Message = (string)o["basicException"][1]?["message"];

            return ret;
        }

    }
}