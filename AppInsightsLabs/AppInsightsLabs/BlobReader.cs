using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AppInsightsLabs
{
    public class BlobReader
    {
        private readonly string _connString;
        private readonly string _containerName;
        private readonly string _containerFolder;

        public BlobReader(string connString, string containerName, string containerFolder = "")
        {
            _connString = connString;
            _containerName = containerName;
            _containerFolder = containerFolder;
        }

        public BlobInfo[] GetBlobInfos()
        {
            var storageAccount = CloudStorageAccount.Parse(_connString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_containerName);
            var blobs = ListBlobs(container).GetAwaiter().GetResult();
            return blobs;
        }
        
        public string[] ToStringsForEveryLine(BlobInfo blobInfo)
        {
            var storageAccount = CloudStorageAccount.Parse(_connString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobRef = blobClient.GetBlobReferenceFromServer(blobInfo.Uri);

            string text;
            using (var memStream = new MemoryStream())
            {
                blobRef.DownloadToStream(memStream);
                text = Encoding.Default.GetString(memStream.ToArray());
            }
            var stringlines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            return stringlines;
        }

        private async Task<BlobInfo[]> ListBlobs(CloudBlobContainer container)
        {
            var result = (await ListBlobsSegments(container))
                .Select(i =>
                {
                    var date = i.Segments[4].Substring(0, i.Segments[4].Length - 1);
                    var time = i.Segments[5].Substring(0, i.Segments[5].Length - 1);

                    var dt = date + " " + time;

                    var start = DateTime.ParseExact(
                        dt,
                        "yyyy-MM-dd HH",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

                    return new BlobInfo
                    {
                        Name = string.Join(string.Empty, i.Segments.Skip(2)),
                        Uri = i,
                        Start = start,
                        End = start.AddHours(1).AddTicks(-1)
                    };
                })
                .ToArray();

            return result;
        }

        private async Task<List<Uri>> ListBlobsSegments(CloudBlobContainer container)
        {
            var resultSegment = string.IsNullOrEmpty(_containerFolder)
                ? await container.ListBlobsSegmentedAsync(string.Empty, true, BlobListingDetails.All, 10, null, null, null)
                : await container.GetDirectoryReference(_containerFolder).ListBlobsSegmentedAsync(true, BlobListingDetails.All, 10, null, null, null);

            var blobItemsToReturn = resultSegment.Results
                .Select(blobItem => blobItem.StorageUri.PrimaryUri)
                .ToList();

            var continuationToken = resultSegment.ContinuationToken;

            while (continuationToken != null)
            {
                resultSegment = await container.ListBlobsSegmentedAsync(
                    string.Empty,
                    true,
                    BlobListingDetails.All,
                    10,
                    continuationToken,
                    null,
                    null);
                
                blobItemsToReturn.AddRange(resultSegment.Results.Select(blobItem => blobItem.StorageUri.PrimaryUri));

                continuationToken = resultSegment.ContinuationToken;
            }

            return blobItemsToReturn;
        }
    }
}