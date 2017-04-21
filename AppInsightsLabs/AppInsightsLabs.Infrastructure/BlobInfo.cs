using System;

namespace AppInsightsLabs.Infrastructure
{
    public class BlobInfo
    {
        /// <summary>
        /// Only the filename
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// blobItem.StorageUri.PrimaryUri
        /// </summary>
        public Uri Uri { get; set; }
        /// <summary>
        /// blobItem.Properties.LastMofified
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }
        /// <summary>
        /// The complete folder path
        /// </summary>
        public string Folder { get; set; }

        public string FolderDatePart => Uri.Segments[4].Trim('/');

        public string FolderHourPart => Uri.Segments[5].Trim('/');
    }
}