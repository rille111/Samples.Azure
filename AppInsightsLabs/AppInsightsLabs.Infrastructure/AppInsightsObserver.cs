using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace AppInsightsLabs.Infrastructure
{
    /// <summary>
    /// TODO: When the timer Ticks, pause the timer to investigate what new traces exist. Then resume.
    /// TODO: I need to debug, why doesnt the logic find new blobs when I know they are there?
    /// TODO: I need to see what happens when a new folder with new blobs are created
    /// TODO: Then, lift this into the WebDashboard in a AI-RealTime-Monitor and add filtering
    /// TODO: As a bonus, create a SignalR hub and consume it with React.
    /// </summary>
    public class AppInsightsObserver
    {
        private readonly AppInsightsCloudBlobReader _blobReader;
        private readonly AppInsightsItemParser _itemParser;

        public List<AppInsightsItemTrace> TraceItems = new List<AppInsightsItemTrace>();
        public delegate void TraceItemsAddedDelegate<in T>(T traces);
        public event TraceItemsAddedDelegate<IEnumerable<AppInsightsItemTrace>> OnTraceItemsAdded;
        private BlobInfo _latestFetchedBlobInfo;

        public AppInsightsObserver(AppInsightsCloudBlobReader blobReader, AppInsightsItemParser itemParser)
        {
            _blobReader = blobReader;
            _itemParser = itemParser;
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

        /// <summary>
        /// TODO: Work needs to be done here:
        /// * Pause the timer when running this method and Resume it
        /// * Doesnt seem to understand when there's a new folder to both add the traces from the old folder AND the new folder
        ///  * It keeps checking the old folder .. or is it?
        /// </summary>
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

        private List<AppInsightsItemTrace> CreateTraceItemsFromBlobs(IEnumerable<BlobInfo> blobs)
        {
            var everyLine = blobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time
            
            var traces = _itemParser.ParseTraceItems(everyLineAsList).ToList();
            TraceItems = traces;
            return traces;
        }
    }
}