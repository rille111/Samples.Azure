using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AppInsightsLabs.Infrastructure.AppInsightsLogParser;
using Newtonsoft.Json.Linq;

namespace AppInsightsLabs
{
    class Program
    {
        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            // Make sure config.json exists, is set to 'Content' and 'Copy Always'.
            var config = JObject.Parse(File.ReadAllText("./config.json"));

            var connString = (string) config["StorageAccountConnectionString"] ;
            var containerName = (string)config["StorageAccountContainerName"]; 
            var containerFolder = (string)config["StorageAccountAppInsightsDumpRootFolder"];
            
            var blobReader = new CloudBlobReader(connString, containerName, containerFolder);

            // Example #1 (polling)
            //StartPollingWithObserver(blobReader);

            // Example #2 (one time read for the latest logs)
            GetTracesForLastDay(blobReader);

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void GetTracesForLastDay(CloudBlobReader blobReader)
        {
            var parser = new AppInsightsItemParser();
            var latestTraceBlob = blobReader.GetLatestBlobInfo("Messages");
            //var allBlobs = blobReader.GetBlobInfosFromFolderAndSubFolders("Messages");
            //var blobs = blobReader.GetBlobInfosFromFolderAndSubFolders($"Messages/{latestTraceBlob.FolderDayPart}/{latestTraceBlob.FolderHourPart}/");
            var blobs = blobReader.GetBlobInfosFromFolderAndSubFolders($"Messages/{latestTraceBlob.FolderDayPart}/");
            var everyLine = blobs.SelectMany(blobReader.ToStringsForEveryLine).ToList();
            var traces = parser.ParseTraceItems(everyLine).ToList();
            
            PrintBlobInfo(traces);

            var first = traces.OrderBy(p => p.TimeStampUtc).First().TimeStampUtc.ToString("yyyy-MM-dd HH:mm:ss");
            var last = traces.OrderBy(p => p.TimeStampUtc).Last().TimeStampUtc.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"Blobs: {blobs.Count}, Traces: {traces.Count}, Earliest-Utc: {first}, Latest-Utc: {last}");
        }

        private static void StartPollingWithObserver(CloudBlobReader blobReader)
        {
            var aiObserver = new AppInsightsObserver(blobReader, new AppInsightsItemParser(), TimeSpan.FromSeconds(1));

            aiObserver.OnEventItemsAdded += PrintBlobInfo;
            aiObserver.OnExceptionItemsAdded += PrintBlobInfo;
            aiObserver.OnTraceItemsAdded += PrintBlobInfo;

            aiObserver.StartPolling<AppInsightsExceptionItem>();
            aiObserver.StartPolling<AppInsightsEventItem>();
            aiObserver.StartPolling<AppInsightsTraceItem>();

            Console.WriteLine("\n\nPress any key to exit ..");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private static void PrintBlobInfo(IEnumerable<AppInsightsItem> items)
        {
            foreach (var item in items)
            {
                Console.WriteLine(item.ToString());
            }
        }
    }
}