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

        public List<AppInsightsTraceItem> TraceItems = new List<AppInsightsTraceItem>();
        public List<AppInsightsEventItem> EventItems = new List<AppInsightsEventItem>();
        public List<AppInsightsExceptionItem> ExceptionItems = new List<AppInsightsExceptionItem>();

        public delegate void TraceItemsAddedDelegate<in T>(T traces);
        public delegate void EventItemsAddedDelegate<in T>(T traces);
        public delegate void ExceptionItemsAddedDelegate<in T>(T traces);

        public event TraceItemsAddedDelegate<IEnumerable<AppInsightsTraceItem>> OnTraceItemsAdded;
        public event EventItemsAddedDelegate<IEnumerable<AppInsightsEventItem>> OnEventItemsAdded;
        public event ExceptionItemsAddedDelegate<IEnumerable<AppInsightsExceptionItem>> OnExceptionItemsAdded;

        private BlobInfo _latestFetchedTraceBlobInfo;
        private BlobInfo _latestFetchedEventBlobInfo;
        private BlobInfo _latestFetchedExceptionBlobInfo;

        private Timer _traceUpdateTimer;
        private Timer _eventUpdateTimer;
        private Timer _exceptionUpdateTimer;

        public AppInsightsObserver(AppInsightsCloudBlobReader blobReader, AppInsightsItemParser itemParser)
        {
            _blobReader = blobReader;
            _itemParser = itemParser;
        }

        #region Traces 
        public void PopulateTracesAndStartTimer(TimeSpan pollingInterval)
        {
            _latestFetchedTraceBlobInfo = _blobReader.GetLatestBlobInfo("Messages");
            var blobs = _blobReader.GetBlobInfosFromFolder(_latestFetchedTraceBlobInfo.Folder);
            AddTraces(blobs);

            _traceUpdateTimer = new Timer(pollingInterval.TotalMilliseconds);
            _traceUpdateTimer.Elapsed += PollTraces;
            _traceUpdateTimer.Enabled = true;
        }

        /// <summary>
        /// TODO: Work needs to be done here:
        /// * Pause the timer when running this method and Resume it
        /// * Doesnt seem to understand when there's a new folder to both add the traces from the old folder AND the new folder
        ///  * It keeps checking the old folder .. or is it?
        /// </summary>
        private void PollTraces(object sender, ElapsedEventArgs e)
        {
            _traceUpdateTimer.Stop();

            var blobs = GetNewerBlobsFromSameFolder(_latestFetchedTraceBlobInfo);
            AddTraces(blobs);

            _traceUpdateTimer.Start();
        }

        private List<BlobInfo> GetNewerBlobsFromSameFolder(BlobInfo latestFetchedTraceBlobInfo)
        {
            // 1: Investigate if new blobs have arrived to the last folder we investigated
            var newBlobsInLastFolder = _blobReader
                .GetBlobInfosFromFolder(latestFetchedTraceBlobInfo.Folder)
                .Where(p => p.LastModified > latestFetchedTraceBlobInfo.LastModified)
                .ToList();
            return newBlobsInLastFolder;
        }

        public void AddTraces(List<BlobInfo> fromBlobs)
        {
            if (!fromBlobs.Any())
                return;

            // 2: Add those
            var everyLine = fromBlobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time

            var traces = _itemParser.ParseTraceItems(everyLineAsList).ToList();
            TraceItems = traces;
            TraceItems.AddRange(traces);
            _latestFetchedTraceBlobInfo = fromBlobs.Last();
            OnTraceItemsAdded?.Invoke(traces);
        }

        #endregion

    }
}