using System;
using System.Collections.Generic;
using System.Linq;

namespace AppInsightsLabs.Infrastructure
{
    public class AppInsightsObserver
    {
        private readonly AppInsightsCloudBlobReader _blobReader;

        public List<TraceItem> TraceItems = new List<TraceItem>();
        public delegate void TraceItemsAddedDelegate<in T>(T traces);
        public event TraceItemsAddedDelegate<IEnumerable<TraceItem>> OnTraceItemsAdded;
        private TraceItem _lasTraceItemAdded;


        public AppInsightsObserver(AppInsightsCloudBlobReader blobReader)
        {
            _blobReader = blobReader;
        }

        public void PopulateTraces()
        {
            var latestBlob = _blobReader.GetLatestBlobInfo("Messages");
            //var blobs = _blobReader.GetBlobInfosFromSubFolder(latestBlob.Folder);
            var blobs = _blobReader.GetBlobInfosFromFolder(latestBlob.Folder);
            var everyLine = blobs.SelectMany(p => _blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time
            var traceParser = new AppInsightsTraceParser();
            var traces = traceParser.ParseFromStrings(everyLineAsList).ToList();
            TraceItems = traces;
            _lasTraceItemAdded = traces.OrderBy(p => p.TimeStampUtc).Last();
            OnTraceItemsAdded?.Invoke(traces);
        }
    }
}