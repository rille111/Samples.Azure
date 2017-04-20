using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppInsightsLabs.Infrastructure
{
    public class TraceItem
    {
        public string SeverityLevel { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string RoleInstance { get; set; }
        public string MessageRaw { get; set; }
        public string MessageTruncated => MessageRaw.PadRight(30).Substring(0, 30);

        public Guid Id { get; set; }
        public Dictionary<string, string> CustomDimensions { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return $"TimeStampUtc: {TimeStampUtc}, SeverityLevel: {SeverityLevel}\n\tMessageTruncated: {MessageTruncated}\n\tCustomDimensions:\n{FlattenCustomDimensions()}";
        }

        private string FlattenCustomDimensions()
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
    }
}