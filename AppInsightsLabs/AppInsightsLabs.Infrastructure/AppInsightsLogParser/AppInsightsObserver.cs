using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private async Task<AiBlobInfo> ProcessItemsReturnLatestFetchedBlobAsync(string topFolder, AiBlobInfo latestAiBlob, Action<List<AiBlobInfo>> itemAddingAction)
        {
            var returnBlob = latestAiBlob;
            if (latestAiBlob == null)
            { // Nothing has been fetched from this folder yet, we will try to do inital population

                //The reason may be: 1. initial populate 2. the folder is missing on the storage 3. no blobs exist in that folder
                latestAiBlob = await _blobReader.GetLatestBlobInfoAsync(topFolder);
                if (latestAiBlob != null)
                {
                    // A blob, time for the initial item-adding!
                    var initalBlobs = await _blobReader.GetBlobInfosFromFolderAsync(latestAiBlob.Folder);
                    itemAddingAction(initalBlobs);
                }
                return latestAiBlob;
            }

            // We already have items, time to populate new items if any
            // First check for remaining blobs in the lastly checked folder and add them
            var newerBlobsSameFolder = await GetNewerBlobsFromSameFolderAsync(latestAiBlob);
            if (newerBlobsSameFolder != null && newerBlobsSameFolder.Any())
            {
                Console.WriteLine("Add new items from same folder as last!");
                itemAddingAction(newerBlobsSameFolder);
                returnBlob = newerBlobsSameFolder.Last();
            }

            // Then check for blobs in the newest folder if any exist, and add them.
            var newerBlobsInOtherFolder = await GetNewerBlobsFromNewestFolderAsync(topFolder, latestAiBlob);
            if (newerBlobsInOtherFolder != null && newerBlobsInOtherFolder.Any())
            {
                Console.WriteLine("Add new items from completely new folder!");
                itemAddingAction(newerBlobsInOtherFolder);
                returnBlob = newerBlobsInOtherFolder.Last();
            }
            return returnBlob;
        }

        private async Task<List<AiBlobInfo>> GetNewerBlobsFromNewestFolderAsync(string topFolder, AiBlobInfo compareAiBlob)
        {
            var latestBlobInNewestFolder = await _blobReader.GetLatestBlobInfoAsync(topFolder);

            if (latestBlobInNewestFolder == null || latestBlobInNewestFolder.LastModified <= compareAiBlob.LastModified)
                return null; // Nothing new

            var blobs = await _blobReader.GetBlobInfosFromFolderAsync(latestBlobInNewestFolder.Folder);
            blobs = blobs.Where(p => p.LastModified > compareAiBlob.LastModified).ToList();
            return blobs;
        }

        private async Task<List<AiBlobInfo>> GetNewerBlobsFromSameFolderAsync(AiBlobInfo latestFetchedTraceAiBlobInfo)
        {
            // 1: Investigate if new blobs have arrived to the last folder we investigated
            var newBlobsInLastFolder = await _blobReader.GetBlobInfosFromFolderAsync(latestFetchedTraceAiBlobInfo.Folder);
            newBlobsInLastFolder = newBlobsInLastFolder.Where(p => p.LastModified > latestFetchedTraceAiBlobInfo.LastModified).ToList();
            return newBlobsInLastFolder;
        }

        #endregion

        #region Exceptions

        private async void PollExceptions(object sender, ElapsedEventArgs e)
        {
            // Stop timer
            var timer = (Timer)sender;
            timer.Stop();

            _latestFetchedExceptionAiBlobInfo = await ProcessItemsReturnLatestFetchedBlobAsync(
                topFolder: ExceptionsFolderName
                , latestAiBlob: _latestFetchedExceptionAiBlobInfo
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

        private async void PollEvents(object sender, ElapsedEventArgs e)
        {
            // Stop timer
            var timer = (Timer)sender;
            timer.Stop();

            _latestFetchedEventAiBlobInfo = await ProcessItemsReturnLatestFetchedBlobAsync(
                topFolder: CustomEventsFolderName
                , latestAiBlob: _latestFetchedEventAiBlobInfo
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

        private async void PollTraces(object sender, ElapsedEventArgs e)
        {
            // Stop timer
            var timer = (Timer)sender;
            timer.Stop();

            _latestFetchedTraceAiBlobInfo = await ProcessItemsReturnLatestFetchedBlobAsync(
                topFolder: TracesFolderName
                , latestAiBlob: _latestFetchedTraceAiBlobInfo
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