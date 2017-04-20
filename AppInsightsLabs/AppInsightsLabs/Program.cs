using System;
using System.Linq;
using AppInsightsLabs.Infrastructure;

namespace AppInsightsLabs
{
    class Program
    {
        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {

            const string connString = "DefaultEndpointsProtocol=https;AccountName=adlaisstordev;AccountKey=4ulhZQVgcDs/HXfWMx8ZKTAMcjc2nHCE97zYrrHSA8xufBJR2Ql++t4Z6eKkRvYa3zDM7s9mdP7xXa/HD67bpQ==;EndpointSuffix=core.windows.net";
            const string containerName = "adlibris-product-ais-appinsights-dump";
            const string containerFolder = "adlibris-product-appinsight-dev_c73480e495214ae0916e8ffbe4587732/Messages/";

            var blobReader = new AppInsightsCloudBlobReader(connString, containerName, containerFolder);
            var aiObserver = new AppInsightsCloudBlobObserver(blobReader);
            aiObserver.BlobInfoAdded += value =>
            {
                foreach (var blobInfo in value)
                {
                    Console.WriteLine(blobInfo.Name);
                }
            };

            aiObserver.AddName("abc");
            aiObserver.AddName("b2");

            var latestBlob = blobReader.GetLatestBlobInfo();
            

            var blobs = blobReader.GetBlobInfosFromSubFolder(latestBlob.Folder);
            var everyLine = blobs.SelectMany(p => blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time
            var traceParser = new AppInsightsTraceParser();
            var traces = traceParser.ParseFromStrings(everyLineAsList);

            //var emptyList = new List<TraceItem>();
            //var observedList = emptyList.ToObservable().ObserveOn(this);

            //emptyList.Add(new TraceItem() {MessageRaw = "heyho"});
            //emptyList.Add(new TraceItem() { MessageRaw = "heyho" });
            //emptyList.Add(new TraceItem() { MessageRaw = "heyho" });





            foreach (var traceItem in traces)
            {
                Console.WriteLine(traceItem);
            }


            Console.WriteLine("\n\nPress any key to exit ..");
            Console.ReadKey();
            Environment.Exit(0);

        }

        private static void OnNext(BlobInfo blobInfo)
        {
            Console.WriteLine("\n" + blobInfo.Name);
        }
    }
}