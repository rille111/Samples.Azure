using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
{
    public class AppInsightsEventItem : AppInsightsItem
    {
        public string Name { get; set; }
        public string NameTruncated => Name.PadRight(30).Substring(0, 30);

        public override string ToString()
        {
            return $"EVENT > TimeStampUtc: {TimeStampUtc}, NameTruncated: {Name}\n\tCustomDimensions:\n{FlattenCustomDimensions()}";
        }

        public static AppInsightsEventItem Create(string jsonString)
        {
            var ret = new AppInsightsEventItem();
            var o = JObject.Parse(jsonString);
            ret.ParseCommon(o);
            ret.Name = (string)o["event"][0]["name"];
            return ret;
        }
    }
}