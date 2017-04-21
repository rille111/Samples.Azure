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
        private BlobInfo _lastTraceItemAddedAsBlobInfo;


        public AppInsightsObserver(AppInsightsCloudBlobReader blobReader, AppInsightsTraceParser traceParser)
        {
            _blobReader = blobReader;
            _traceParser = traceParser;
        }

        public void PopulateTracesAndStartTimer(TimeSpan pollingInterval)
        {
            _lastTraceItemAddedAsBlobInfo = _blobReader.GetLatestBlobInfo("Messages");
            var blobs = _blobReader.GetBlobInfosFromFolder(_lastTraceItemAddedAsBlobInfo.Folder);
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
                .GetBlobInfosFromFolder(_lastTraceItemAddedAsBlobInfo.Folder)
                .Where(p => p.LastModified > _lastTraceItemAddedAsBlobInfo.LastModified)
                .ToList();

            if (newBlobsInLastFolder.Any())
            {
                // 2: Add those
                var newTraces = CreateTraceItemsFromBlobs(newBlobsInLastFolder);
                TraceItems.AddRange(newTraces);
                _lastTraceItemAddedAsBlobInfo = newBlobsInLastFolder.Last();
                OnTraceItemsAdded?.Invoke(newTraces);
            }
            else
            {
                return;
            }


            // 3: See if there's a new hour-folder
            // 4: Add those, and set last folder
            // 5. See if there's a new day-folder
            // 6. Get the last hour-folder from that day-folder
            // 7. Add those
            // 8. And set the last folder
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