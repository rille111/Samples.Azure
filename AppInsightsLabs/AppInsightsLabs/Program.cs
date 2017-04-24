using System;
using System.IO;
using AppInsightsLabs.Infrastructure;
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

            var blobReader = new AppInsightsCloudBlobReader(connString, containerName, containerFolder);
            var aiObserver = new AppInsightsObserver(blobReader, new AppInsightsItemParser());
            aiObserver.OnTraceItemsAdded += blobInfos =>
            {
                foreach (var blobInfo in blobInfos)
                {
                    Console.WriteLine(blobInfo.ToString());
                }
            };

            aiObserver.PopulateTracesAndStartTimer(TimeSpan.FromSeconds(1));

            Console.WriteLine("\n\nPress any key to exit ..");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}