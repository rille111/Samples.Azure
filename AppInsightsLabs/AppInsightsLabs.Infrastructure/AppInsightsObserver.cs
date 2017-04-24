using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace AppInsightsLabs.Infrastructure
{
    /// <summary>
    /// TODO: I need to debug, why doesnt the logic find new blobs when I know they are there?
    /// TODO: I need to see what happens when a new folder with new blobs are created
    /// TODO: Then, lift this into the WebDashboard in a AI-RealTime-Monitor and add filtering
    /// TODO: As a bonus, create a SignalR hub and consume it with React.
    /// </summary>
    public class AppInsightsObserver
    {
        public string TracesFolderName = "Messages";
        public string CustomEventsFolderName = "Event";
        public string ExceptionsFolderName = "Exceptions";

        private readonly AppInsightsCloudBlobReader _blobReader;
        private readonly AppInsightsItemParser _itemParser;
        private readonly TimeSpan _pollingInterval;

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

        public AppInsightsObserver(AppInsightsCloudBlobReader blobReader, AppInsightsItemParser itemParser, TimeSpan pollingInterval)
        {
            _blobReader = blobReader;
            _itemParser = itemParser;
            _pollingInterval = pollingInterval;
        }


        #region Common

        public void StartPolling<TWhatType>() where TWhatType : AppInsightsItem, new()
        {
            if (typeof(TWhatType) == typeof(AppInsightsTraceItem))
            {
                _traceUpdateTimer = new Timer(_pollingInterval.TotalMilliseconds);
                _traceUpdateTimer.Elapsed += PollTraces;
                _traceUpdateTimer.Start();
            }

            if (typeof(TWhatType) == typeof(AppInsightsEventItem))
            {
                _eventUpdateTimer = new Timer(_pollingInterval.TotalMilliseconds);
                _eventUpdateTimer.Elapsed += PollEvents;
                _eventUpdateTimer.Start();
            }

            if (typeof(TWhatType) == typeof(AppInsightsExceptionItem))
            {
                _exceptionUpdateTimer = new Timer(_pollingInterval.TotalMilliseconds);
                _exceptionUpdateTimer.Elapsed += PollExceptions;
                _exceptionUpdateTimer.Start();
            }
        }

        private void ProcessItems(string inFolder, ref BlobInfo latestBlob, Action<List<BlobInfo>> itemAddingAction)
        {
            if (latestBlob == null)
            {
                // We haven't gotten any exceptions logs yet, whatever the reason (initial load, folder 'Exceptions' is missing, or no blobs are in that folder)
                latestBlob = _blobReader.GetLatestBlobInfo(inFolder);
                if (latestBlob != null)
                {
                    // Got at least one blob, for the inital load
                    var initalBlobs = _blobReader.GetBlobInfosFromFolder(latestBlob.Folder);
                    itemAddingAction(initalBlobs);
                }
            }
            else
            {
                // We have already gotten the inital blobs, investigate the latest to see if any new have arrived
                var newerBlobs = GetNewerBlobsFromSameFolder(latestBlob);
                if (newerBlobs.Any())
                {
                    itemAddingAction(newerBlobs);
                    latestBlob = newerBlobs.Last();
                }
            }
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

        #endregion

        #region Exceptions

        private void PollExceptions(object sender, ElapsedEventArgs e)
        {
            // Stop timer
            var timer = (Timer) sender;
            timer.Stop();

            ProcessItems(
                inFolder: ExceptionsFolderName
                , latestBlob: ref _latestFetchedExceptionBlobInfo
                , itemAddingAction: AddExceptions);

            // Resume timer
            timer.Start();
        }

        private void AddExceptions(List<BlobInfo> fromBlobs)
        {
            if (!fromBlobs.Any())
                return;

            // 2: Add those
            var everyLine = fromBlobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time

            var exceptions = _itemParser.ParseExceptionItems(everyLineAsList).ToList();
            ExceptionItems.AddRange(exceptions);
            OnExceptionItemsAdded?.Invoke(exceptions);
        }

        #endregion

        #region Events

        private void PollEvents(object sender, ElapsedEventArgs e)
        {
            // Stop timer
            var timer = (Timer)sender;
            timer.Stop();

            ProcessItems(
                inFolder: CustomEventsFolderName
                , latestBlob: ref _latestFetchedEventBlobInfo
                , itemAddingAction: AddEvents);

            // Resume timer
            timer.Start();
        }

        private void AddEvents(List<BlobInfo> fromBlobs)
        {
            if (!fromBlobs.Any())
                return;

            // 2: Add those
            var everyLine = fromBlobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time

            var events = _itemParser.ParseEventItems(everyLineAsList).ToList();
            EventItems.AddRange(events);
            OnEventItemsAdded?.Invoke(events);
        }

        #endregion

        #region Traces 

        private void PollTraces(object sender, ElapsedEventArgs e)
        {
            // Stop timer
            var timer = (Timer)sender;
            timer.Stop();

            ProcessItems(
                inFolder: TracesFolderName
                , latestBlob: ref _latestFetchedTraceBlobInfo
                , itemAddingAction: AddTraces);

            // Resume timer
            timer.Start();
        }

        private void AddTraces(List<BlobInfo> fromBlobs)
        {
            if (!fromBlobs.Any())
                return;

            // 2: Add those
            var everyLine = fromBlobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time

            var traces = _itemParser.ParseTraceItems(everyLineAsList).ToList();
            TraceItems.AddRange(traces);
            OnTraceItemsAdded?.Invoke(traces);
        }

        #endregion
    }
}

//#region Traces 

//public void PopulateTracesAndStartTimer(TimeSpan pollingInterval)
//{
//    _latestFetchedTraceBlobInfo = _blobReader.GetLatestBlobInfo("Messages");
//    var blobs = _blobReader.GetBlobInfosFromFolder(_latestFetchedTraceBlobInfo.Folder);
//    AddTraces(blobs);

//    _traceUpdateTimer = new Timer(pollingInterval.TotalMilliseconds);
//    _traceUpdateTimer.Elapsed += PollTraces;
//    _traceUpdateTimer.Enabled = true;
//}

//private void PollTraces(object sender, ElapsedEventArgs e)
//{
//    _traceUpdateTimer.Stop();

//    var blobs = GetNewerBlobsFromSameFolder(_latestFetchedTraceBlobInfo);
//    AddTraces(blobs);

//    _traceUpdateTimer.Start();
//}

//private void AddTraces(List<BlobInfo> fromBlobs)
//{
//    if (!fromBlobs.Any())
//        return;

//    // 2: Add those
//    var everyLine = fromBlobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
//    var everyLineAsList = everyLine.ToList(); // Takes time

//    var traces = _itemParser.ParseTraceItems(everyLineAsList).ToList();
//    TraceItems.AddRange(traces);
//    _latestFetchedTraceBlobInfo = fromBlobs.Last();
//    OnTraceItemsAdded?.Invoke(traces);
//}

//#endregion