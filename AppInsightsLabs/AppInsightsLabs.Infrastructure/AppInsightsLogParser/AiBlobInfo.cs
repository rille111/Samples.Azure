using System;
using System.Linq;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
{
    public class AiBlobInfo
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
        /// <summary>
        /// The complete folder path up to the day, excluding the hour
        /// </summary>
        public string FolderDay => string.Join(string.Empty, Uri.Segments.Skip(2).Take(3)).Trim('/');

    }
}