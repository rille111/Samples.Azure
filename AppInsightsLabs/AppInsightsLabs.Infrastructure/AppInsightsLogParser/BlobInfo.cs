using System;
using System.Linq;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
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
        /// <summary>
        /// Only the day part. May contain '/'. Example "2004-12-31"
        /// </summary>
        public string FolderDayPart => string.Join(string.Empty, Uri.Segments.Skip(4).Take(1)).Trim('/');
        /// <summary>
        /// Only the day part. May contain '/'. Example "2004-12-31"
        /// </summary>
        public string FolderHourPart => string.Join(string.Empty, Uri.Segments.Skip(5).Take(1)).Trim('/');

    }
}