using System;

namespace AppInsightsLabs
{
    public class BlobInfo
    {
        public string Name;
        //public DateTime Start;
        //public DateTime End;
        /// <summary>
        /// blobItem.StorageUri.PrimaryUri
        /// </summary>
        public Uri Uri { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}