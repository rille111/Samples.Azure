using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace AppInsightsLabs.Infrastructure
{
    public class AppInsightsObserver
    {
        private readonly AppInsightsCloudBlobReader _blobReader;
        private readonly AppInsightsTraceParser _traceParser;

        public List<TraceItem> TraceItems = new List<TraceItem>();
        public delegate void TraceItemsAddedDelegate<in T>(T traces);
        public event TraceItemsAddedDelegate<IEnumerable<TraceItem>> OnTraceItemsAdded;
        private BlobInfo _latestFetchedBlobInfo;


        public AppInsightsObserver(AppInsightsCloudBlobReader blobReader, AppInsightsTraceParser traceParser)
        {
            _blobReader = blobReader;
            _traceParser = traceParser;
        }

        public void PopulateTracesAndStartTimer(TimeSpan pollingInterval)
        {
            _latestFetchedBlobInfo = _blobReader.GetLatestBlobInfo("Messages");
            var blobs = _blobReader.GetBlobInfosFromFolder(_latestFetchedBlobInfo.Folder);
            var traces = CreateTraceItemsFromBlobs(blobs);
            TraceItems = traces;
            OnTraceItemsAdded?.Invoke(traces);

            Timer updateTimer = new Timer(pollingInterval.TotalMilliseconds);
            updateTimer.Elapsed += PollTraces;
            updateTimer.Enabled = true;

        }

        private void PollTraces(object sender, ElapsedEventArgs e)
        {
            CheckForNewTraces();
        }

        public void CheckForNewTraces()
        {
            // 1: Investigate if new blobs have arrived to the last folder we investigated
            var newBlobsInLastFolder = _blobReader
                .GetBlobInfosFromFolder(_latestFetchedBlobInfo.Folder)
                .Where(p => p.LastModified > _latestFetchedBlobInfo.LastModified)
                .ToList();

            if (newBlobsInLastFolder.Any())
            {
                // 2: Add those
                var newTraces = CreateTraceItemsFromBlobs(newBlobsInLastFolder);
                TraceItems.AddRange(newTraces);
                _latestFetchedBlobInfo = newBlobsInLastFolder.Last();
                OnTraceItemsAdded?.Invoke(newTraces);
            }
            else
            {
                
            }


            // 3: See if there's a new hour-folder in the storage based on the absolue newest blob
            var absoluteNewestBlob = _blobReader.GetLatestBlobInfo("Messages");
            if (absoluteNewestBlob.FolderHourPart != _latestFetchedBlobInfo.FolderHourPart)
            {
                // 4: Add those, and set last folder
                var newBlobsInNewFolder = _blobReader
                    .GetBlobInfosFromFolder(absoluteNewestBlob.Folder)
                    .Where(p => p.LastModified > _latestFetchedBlobInfo.LastModified)
                    .ToList();
                var evenNewerTraces = CreateTraceItemsFromBlobs(newBlobsInNewFolder);
                TraceItems.AddRange(evenNewerTraces);
                _latestFetchedBlobInfo = absoluteNewestBlob;
                OnTraceItemsAdded?.Invoke(evenNewerTraces);
            }
        }

        private List<TraceItem> CreateTraceItemsFromBlobs(IEnumerable<BlobInfo> blobs)
        {
            var everyLine = blobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time
            
            var traces = _traceParser.ParseFromStrings(everyLineAsList).ToList();
            TraceItems = traces;
            return traces;
        }
    }
}