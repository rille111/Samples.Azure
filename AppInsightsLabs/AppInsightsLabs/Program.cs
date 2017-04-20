using System;
using System.Linq;

namespace AppInsightsLabs
{
    class Program
    {
        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            const string connString = "";
            const string containerName = "adlibris-product-ais-appinsights-dump";

            var blobReader = new BlobReader(connString, containerName);

            var blobs = blobReader.GetBlobInfos("");
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