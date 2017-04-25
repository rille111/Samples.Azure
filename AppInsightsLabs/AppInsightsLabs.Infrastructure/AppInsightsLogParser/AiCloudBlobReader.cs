using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
{
    // ReSharper disable RedundantArgumentDefaultValue
    public class AiCloudBlobReader
    {
        private readonly string _rootFolder;
        private readonly CloudBlobClient _blobClient;
        private readonly CloudBlobContainer _container;

        public AiCloudBlobReader(string connString, string containerName, string rootFolder = "")
        {
            _rootFolder = rootFolder.Trim(new []{'/', '\\'});

            var storageAccount = CloudStorageAccount.Parse(connString);
            _blobClient = storageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference(containerName);
        }

        /// <summary>
        /// Get info for all blobs in the entire container. Use with care!
        /// </summary>
        public List<AiBlobInfo> GetAllBlobInfos()
        {
            return ListBlobsAsync(_rootFolder).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get infos for all blobs in a given folder.
        /// </summary>
        public List<AiBlobInfo> GetBlobInfosFromFolder(string folder)
        {
            return ListBlobsAsync($"{folder}").ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get infos for all blobs in a given folder and its sub folders.
        /// </summary>
        public async Task<List<AiBlobInfo>> GetBlobInfosFromFolderAndSubFoldersAsync(string topFolder)
        {
            var ret = new List<AiBlobInfo>();
            var allFolders = BuildFlatRecursiveFolderList(topFolder);
            var tasks = new List<Task<List<AiBlobInfo>>>();
            allFolders.Add(topFolder);
            foreach (var currentFolder in allFolders)
            {
                var t = ListBlobsAsync(currentFolder);
                tasks.Add(t);
            }

            var blobInfoList = await Task.WhenAll(tasks);

            foreach (var blobInfos in blobInfoList)
            {
                ret.AddRange(blobInfos);
            }
            return ret;
        }

        private List<string> BuildFlatRecursiveFolderList(string folder)
        {
            var ret = new List<string>();
            var subFoldersBlobs = _container.GetDirectoryReference(folder)
                .ListBlobs(false, BlobListingDetails.None)
                .Where(b => b is CloudBlobDirectory)
                .ToList();

            foreach (var folderBlob in subFoldersBlobs)
            {
                var folderString = string.Join(string.Empty, folderBlob.Uri.Segments.Skip(2));
                ret.Add(folderString);

                var foldersToAdd = BuildFlatRecursiveFolderList(folderString);
                ret.AddRange(foldersToAdd);
            }
            return ret;
        }

        /// <summary>
        /// 1. Scans folder names that are expected to follow this format: /[yyyy-MM-dd]/[HH]
        /// 2. Chooses the 'newest' folder
        /// 3. Scans the files
        /// </summary>
        /// <param name="eventTypeFolder">e.g. "Messages" or "Exceptions" </param>
        public AiBlobInfo GetLatestBlobInfo(string eventTypeFolder)
        {
            var folder = $"{_rootFolder}/{eventTypeFolder}";
            
            var blobs = _container.GetDirectoryReference(folder).ListBlobs(false, BlobListingDetails.None);
            var folders = blobs.Where(b => b is CloudBlobDirectory).ToList();
            var folderDates = new List<DateTime>();

            if (folders.Count == 0)
                return null;

            // Find out last day
            foreach (var subFolder in folders)
            {
                var datePartOfFolder = subFolder.Uri.Segments.Last().Trim('/');
                var folderDate = DateTime.ParseExact(datePartOfFolder, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                folderDates.Add(folderDate);
            }
            var lastDay = folderDates.OrderBy(p => p).Last();
            var lastDayFolder = $"{folder}/{lastDay.ToString("yyyy-MM-dd")}";

            // Find out last hour of that day
            var folderWithHours = _container.GetDirectoryReference(lastDayFolder).ListBlobs(false, BlobListingDetails.None);
            var folderHours = new List<int>();
            foreach (var subFolder in folderWithHours)
            {
                var hourPartOfFolder = subFolder.Uri.Segments.Last().Trim('/');
                var folderHour = int.Parse(hourPartOfFolder);
                folderHours.Add(folderHour);
            }
            var lastHour = folderHours.OrderBy(p => p).Last();

            // Determine newest day/hour folder
            var lastDayHourFolder =  $"{folder}/{lastDay.ToString("yyyy-MM-dd")}/{lastHour.ToString().PadLeft(2, '0')}";

            // List the blobs and choose the newest
            var blobsInThatFolder = ListBlobsAsync(lastDayHourFolder).ConfigureAwait(false).GetAwaiter().GetResult();
            return blobsInThatFolder.OrderBy(p => p.LastModified).Last();
        }

        public string[] ToStringsForEveryLine(AiBlobInfo aiBlobInfo)
        {
            var blobRef = _blobClient.GetBlobReferenceFromServer(aiBlobInfo.Uri);

            string text;
            using (var memStream = new MemoryStream())
            {
                blobRef.DownloadToStream(memStream);
                text = Encoding.Default.GetString(memStream.ToArray());
            }
            var stringlines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            return stringlines;
        }

        private async Task<List<AiBlobInfo>> ListBlobsAsync(string folder)
        {
            var cloudDirectory = _container.GetDirectoryReference(folder);
            var resultSegment = await cloudDirectory.ListBlobsSegmentedAsync(true, BlobListingDetails.All, 10, null, null, null);

            var blobItemsToReturn = resultSegment
                .Results
                .OfType<CloudBlockBlob>()
                .Where(p => p.Properties?.LastModified != null)
                .OrderBy(p => p.Properties.LastModified.Value)
                .Select(CreateBlobInfo).ToList();

            var continuationToken = resultSegment.ContinuationToken;

            while (continuationToken != null)
            {
                resultSegment = await cloudDirectory.ListBlobsSegmentedAsync(true,BlobListingDetails.All,10,continuationToken,null,null);

                var range = resultSegment.Results
                .OfType<CloudBlockBlob>()
                .Where(p => p.Properties?.LastModified != null)
                .Select(CreateBlobInfo).ToList();

                blobItemsToReturn.AddRange(range);
                continuationToken = resultSegment.ContinuationToken;
            }

            return blobItemsToReturn;
        }

        private AiBlobInfo CreateBlobInfo(CloudBlockBlob blobItem)
        {
            return new AiBlobInfo
            {
                FileName = blobItem.StorageUri.PrimaryUri.Segments.Last(),
                Uri = blobItem.StorageUri.PrimaryUri,
                Folder = string.Join(string.Empty, blobItem.StorageUri.PrimaryUri.Segments.Skip(2).Take(4)),
                LastModified = blobItem.Properties?.LastModified
            };
        }

        // Saving the uri-date parsing stuff if i ever would need these Start / End properties. Dunno why though.
        //private async Task<BlobInfo[]> ListBlobs(string folder)
        //{
        //    var result = (await ListBlobsSegments(folder))
        //        .Select(i =>
        //        {
        //            //var date = i.Segments[4].Substring(0, i.Segments[4].Length - 1);
        //            //var time = i.Segments[5].Substring(0, i.Segments[5].Length - 1);

        //            //var dt = date + " " + time;

        //            //var start = DateTime.ParseExact(
        //            //    dt,
        //            //    "yyyy-MM-dd HH",
        //            //    CultureInfo.InvariantCulture,
        //            //    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        //            return new BlobInfo
        //            {
        //                Name = string.Join(string.Empty, i.Segments.Skip(2)),
        //                Uri = i,
        //                //LastModified
        //                //Start = start,
        //                //End = start.AddHours(1).AddTicks(-1)
        //            };
        //        })
        //        .ToArray();

        //    return result;
        //}
    }
}