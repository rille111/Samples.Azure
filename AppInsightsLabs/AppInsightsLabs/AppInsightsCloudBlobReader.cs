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
    public class AppInsightsCloudBlobReader
    {
        private readonly string _connString;
        private readonly string _containerName;
        private readonly string _containerFolder;
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _container;

        public AppInsightsCloudBlobReader(string connString, string containerName, string containerFolder = "")
        {
            _connString = connString;
            _containerName = containerName;
            _containerFolder = containerFolder;

            _storageAccount = CloudStorageAccount.Parse(_connString);
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference(_containerName);
        }

        public BlobInfo[] GetAllBlobInfos()
        {
            var blobs = ListBlobs(_containerFolder).GetAwaiter().GetResult();
            return blobs;
        }

        public BlobInfo[] GetBlobInfosFromSubFolder(string folder)
        {
            var blobs = ListBlobs(folder).GetAwaiter().GetResult();
            return blobs;
        }

        public BlobInfo GetLatestBlobInfo()
        {
            var blobs = _container.GetDirectoryReference(_containerFolder).ListBlobs(false, BlobListingDetails.None);
            var folders = blobs.Where(b => b is CloudBlobDirectory).ToList();
            var folderDates = new List<DateTime>();

            // Find out last day
            foreach (var folder in folders)
            {
                var datePartOfFolder = folder.Uri.Segments.Last().Trim('/');
                var folderDate = DateTime.ParseExact(datePartOfFolder, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                folderDates.Add(folderDate);
            }
            var lastDay = folderDates.OrderBy(p => p).Last();
            var lastDayFolder = _containerFolder + $"{lastDay.ToString("yyyy-MM-dd")}";

            // Find out last hour of that day
            var folderWithHours = _container.GetDirectoryReference(lastDayFolder).ListBlobs(false, BlobListingDetails.None);
            var folderHours = new List<int>();
            foreach (var folder in folderWithHours)
            {
                var hourPartOfFolder = folder.Uri.Segments.Last().Trim('/');
                var folderHour = int.Parse(hourPartOfFolder);
                folderHours.Add(folderHour);
            }
            var lastHour = folderHours.OrderBy(p => p).Last();

            // Find out last blob in that day/hour folder
            var lastDayHourFolder = _containerFolder + $"{lastDay.ToString("yyyy-MM-dd")}/{lastHour.ToString().PadLeft(2, '0')}";

            // Get the latest blob in that folder!
            var newestBlob = _container.GetDirectoryReference(lastDayHourFolder)
                .ListBlobs(false, BlobListingDetails.None)
                .OfType<CloudBlockBlob>()
                .Where(p => p.Properties?.LastModified != null)
                .OrderBy(p => p.Properties.LastModified.Value)
                .Last();

            return new BlobInfo
            {
                Name = string.Join(string.Empty, newestBlob.StorageUri.PrimaryUri.Segments.Skip(2)),
                Uri = newestBlob.StorageUri.PrimaryUri,
                LastModified = newestBlob.Properties?.LastModified
                //Start = start,
                //End = start.AddHours(1).AddTicks(-1)
            };


            //var blobsInLatestFolder = _container.GetDirectoryReference(lastDayHourFolder)
            //        .ListBlobs(false, BlobListingDetails.None)
            //        .OfType<CloudBlockBlob>()
            //        .Where(p => p.Properties?.LastModified != null)
            //        .OrderBy(p => p.Properties.LastModified.Value)
            //        .ToList();

            //var blobTimeStamps = blobsInLatestFolder.Select(blob => blob.Properties.LastModified.Value).ToList();
            //var latestTimeStamp = blobTimeStamps.OrderBy(p => p).Last();

            //var latestBlob = blobsInLatestFolder

            //return latestTimeStamp;
        }

        public string[] ToStringsForEveryLine(BlobInfo blobInfo)
        {
            var blobRef = _blobClient.GetBlobReferenceFromServer(blobInfo.Uri);

            string text;
            using (var memStream = new MemoryStream())
            {
                blobRef.DownloadToStream(memStream);
                text = Encoding.Default.GetString(memStream.ToArray());
            }
            var stringlines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            return stringlines;
        }

        private async Task<BlobInfo[]> ListBlobs(string folder)
        {
            var result = (await ListBlobsSegments(folder))
                .Select(i =>
                {
                    //var date = i.Segments[4].Substring(0, i.Segments[4].Length - 1);
                    //var time = i.Segments[5].Substring(0, i.Segments[5].Length - 1);

                    //var dt = date + " " + time;

                    //var start = DateTime.ParseExact(
                    //    dt,
                    //    "yyyy-MM-dd HH",
                    //    CultureInfo.InvariantCulture,
                    //    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

                    return new BlobInfo
                    {
                        Name = string.Join(string.Empty, i.Segments.Skip(2)),
                        Uri = i,
                        //LastModified
                        //Start = start,
                        //End = start.AddHours(1).AddTicks(-1)
                    };
                })
                .ToArray();

            return result;
        }

        /// <summary>
        /// Returns list of blobItem.StorageUri.PrimaryUri
        /// </summary>
        private async Task<List<Uri>> ListBlobsSegments(string folder)
        {
            var resultSegment = await _container.GetDirectoryReference(folder).ListBlobsSegmentedAsync(true, BlobListingDetails.All, 10, null, null, null);

            var blobItemsToReturn = resultSegment.Results
                .Select(blobItem => blobItem.StorageUri.PrimaryUri)
                .ToList();

            var continuationToken = resultSegment.ContinuationToken;

            while (continuationToken != null)
            {
                resultSegment = await _container.ListBlobsSegmentedAsync(
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