using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.Infrastructure
{
    public interface IAppInsightsItem { }

    public abstract class AppInsightsItem
    {
        public DateTime TimeStampUtc { get; set; }
        public string RoleInstance { get; set; }
        public Guid Id { get; set; }
        public Dictionary<string, string> CustomDimensions { get; set; } = new Dictionary<string, string>();
        public string FlattenCustomDimensions()
        {
            if (CustomDimensions.Any())
            {
                var stringBuilder = new StringBuilder();
                foreach (var customDimension in CustomDimensions)
                {
                    stringBuilder.AppendLine($"\t\t{customDimension.Key}: {customDimension.Value}");
                }
                return stringBuilder.ToString();
            }
            return string.Empty;
        }

        protected virtual void ParseCommon(JObject o)
        {
            TimeStampUtc = DateTime.Parse((string)o["context"]["data"]["eventTime"]);
            RoleInstance = (string)o["context"]["device"]["roleInstance"];
            Id = (Guid)o["internal"]["data"]["id"];

            var customDims = o["context"]?["custom"]?["dimensions"] as JArray; // array

            if (customDims != null)
            {
                foreach (var jToken in customDims.Children())
                {
                    var jProp = (JProperty)jToken.First();
                    CustomDimensions.Add(jProp.Name, jProp.Value.ToString());
                }
            }
        }
    }
}
