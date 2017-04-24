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

        private readonly AiCloudBlobReader _blobReader;
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

        private AiBlobInfo _latestFetchedTraceAiBlobInfo;
        private AiBlobInfo _latestFetchedEventAiBlobInfo;
        private AiBlobInfo _latestFetchedExceptionAiBlobInfo;

        private Timer _traceUpdateTimer;
        private Timer _eventUpdateTimer;
        private Timer _exceptionUpdateTimer;

        public AppInsightsObserver(AiCloudBlobReader blobReader, AppInsightsItemParser itemParser, TimeSpan pollingInterval)
        {
            _blobReader = blobReader;
            _itemParser = itemParser;
            _pollingInterval = pollingInterval;
        }


        #region Common

        /// <summary>
        /// The first events that you hookup will give you items from the latest hour and then keep polling for more.
        /// </summary>
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

        private void ProcessItems(string topFolder, ref AiBlobInfo latestAiBlob, Action<List<AiBlobInfo>> itemAddingAction)
        {
            if (latestAiBlob == null)
            { // Nothing has been fetched from this folder yet, we will try to do inital population
                
                //The reason may be: 1. initial populate 2. the folder is missing on the storage 3. no blobs exist in that folder
                latestAiBlob = _blobReader.GetLatestBlobInfo(topFolder);
                if (latestAiBlob != null)
                {
                    // A blob, time for the initial item-adding!
                    var initalBlobs = _blobReader.GetBlobInfosFromFolder(latestAiBlob.Folder);
                    itemAddingAction(initalBlobs);
                }
            }
            else
            { // We already have items, time to populate new items if any

                // First check for remaining blobs in the lastly checked folder and add them
                var newerBlobsSameFolder = GetNewerBlobsFromSameFolder(latestAiBlob);
                if (newerBlobsSameFolder != null && newerBlobsSameFolder.Any())
                {
                    Console.WriteLine("Add new items from same folder as last!");
                    itemAddingAction(newerBlobsSameFolder);
                    latestAiBlob = newerBlobsSameFolder.Last();
                }

                // Then check for blobs in the newest folder if any exist, and add them.
                var newerBlobsInOtherFolder = GetNewerBlobsFromNewestFolder(topFolder, latestAiBlob);
                if (newerBlobsInOtherFolder != null && newerBlobsInOtherFolder.Any())
                {
                    Console.WriteLine("Add new items from completely new folder!");
                    itemAddingAction(newerBlobsInOtherFolder);
                    latestAiBlob = newerBlobsInOtherFolder.Last();
                }
            }
        }

        private List<AiBlobInfo> GetNewerBlobsFromNewestFolder(string topFolder, AiBlobInfo compareAiBlob)
        {
            var latestBlobInNewestFolder = _blobReader.GetLatestBlobInfo(topFolder);

            if (latestBlobInNewestFolder == null || latestBlobInNewestFolder.LastModified <= compareAiBlob.LastModified)
                return null; // Nothing new

            var blobs = _blobReader
                .GetBlobInfosFromFolder(latestBlobInNewestFolder.Folder)
                .Where(p => p.LastModified > compareAiBlob.LastModified)
                .ToList();
            return blobs;
        }

        private List<AiBlobInfo> GetNewerBlobsFromSameFolder(AiBlobInfo latestFetchedTraceAiBlobInfo)
        {
            // 1: Investigate if new blobs have arrived to the last folder we investigated
            var newBlobsInLastFolder = _blobReader
                .GetBlobInfosFromFolder(latestFetchedTraceAiBlobInfo.Folder)
                .Where(p => p.LastModified > latestFetchedTraceAiBlobInfo.LastModified)
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
                , latestAiBlob: ref _latestFetchedExceptionAiBlobInfo
                , itemAddingAction: AddExceptions);

            // Resume timer
            timer.Start();
        }

        private void AddExceptions(List<AiBlobInfo> fromBlobs)
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
                , latestAiBlob: ref _latestFetchedEventAiBlobInfo
                , itemAddingAction: AddEvents);

            // Resume timer
            timer.Start();
        }

        private void AddEvents(List<AiBlobInfo> fromBlobs)
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
                , latestAiBlob: ref _latestFetchedTraceAiBlobInfo
                , itemAddingAction: AddTraces);

            // Resume timer
            timer.Start();
        }

        private void AddTraces(List<AiBlobInfo> fromBlobs)
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