using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace AppInsightsLabs.Infrastructure.AppInsightsLogParser
{
    /// <summary>
    /// 
    /// </summary>
    public class AppInsightsObserver
    {
        public string TracesFolderName = "Messages";
        public string CustomEventsFolderName = "Event";
        public string ExceptionsFolderName = "Exceptions";

        private readonly CloudBlobReader _blobReader;
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

        public AppInsightsObserver(CloudBlobReader blobReader, AppInsightsItemParser itemParser, TimeSpan pollingInterval)
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

        private void ProcessItems(string topFolder, ref BlobInfo latestBlob, Action<List<BlobInfo>> itemAddingAction)
        {
            if (latestBlob == null)
            { // Nothing has been fetched from this folder yet, we will try to do inital population
                
                //The reason may be: 1. initial populate 2. the folder is missing on the storage 3. no blobs exist in that folder
                latestBlob = _blobReader.GetLatestBlobInfo(topFolder);
                if (latestBlob != null)
                {
                    // A blob, time for the initial item-adding!
                    var initalBlobs = _blobReader.GetBlobInfosFromFolder(latestBlob.Folder);
                    itemAddingAction(initalBlobs);
                }
            }
            else
            { // We already have items, time to populate new items if any

                // First check for remaining blobs in the lastly checked folder and add them
                var newerBlobsSameFolder = GetNewerBlobsFromSameFolder(latestBlob);
                if (newerBlobsSameFolder != null && newerBlobsSameFolder.Any())
                {
                    Console.WriteLine("Add new items from same folder as last!");
                    itemAddingAction(newerBlobsSameFolder);
                    latestBlob = newerBlobsSameFolder.Last();
                }

                // Then check for blobs in the newest folder if any exist, and add them.
                var newerBlobsInOtherFolder = GetNewerBlobsFromNewestFolder(topFolder, latestBlob);
                if (newerBlobsInOtherFolder != null && newerBlobsInOtherFolder.Any())
                {
                    Console.WriteLine("Add new items from completely new folder!");
                    itemAddingAction(newerBlobsInOtherFolder);
                    latestBlob = newerBlobsInOtherFolder.Last();
                }
            }
        }

        private List<BlobInfo> GetNewerBlobsFromNewestFolder(string topFolder, BlobInfo compareBlob)
        {
            var latestBlobInNewestFolder = _blobReader.GetLatestBlobInfo(topFolder);

            if (latestBlobInNewestFolder == null || latestBlobInNewestFolder.LastModified <= compareBlob.LastModified)
                return null; // Nothing new

            var blobs = _blobReader
                .GetBlobInfosFromFolder(latestBlobInNewestFolder.Folder)
                .Where(p => p.LastModified > compareBlob.LastModified)
                .ToList();
            return blobs;
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
                topFolder: ExceptionsFolderName
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
                topFolder: CustomEventsFolderName
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
                topFolder: TracesFolderName
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