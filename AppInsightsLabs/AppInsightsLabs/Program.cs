using System;
using System.Linq;

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
            var latestBlob = blobReader.GetLatestBlobInfo();
            var blobs = blobReader.GetBlobInfosFromSubFolder(latestBlob.Folder);
            var everyLine = blobs.SelectMany(p => blobReader.ToStringsForEveryLine(p));
            var everyLineAsList = everyLine.ToList(); // Takes time
            var traceParser = new AppInsightsTraceParser();
            var traces = traceParser.ParseFromStrings(everyLineAsList);

            foreach (var traceItem in traces)
            {
                Console.WriteLine(traceItem);
            }

            Console.WriteLine("\n\nPress any key to exit ..");
            Console.ReadKey();
        }
    }
}