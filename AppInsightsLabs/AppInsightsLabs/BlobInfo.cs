using System;

namespace AppInsightsLabs
{
    public class BlobInfo
    {
        public string Name;
        public DateTime Start;
        public DateTime End;
        public Uri Uri { get; set; }
    }
}