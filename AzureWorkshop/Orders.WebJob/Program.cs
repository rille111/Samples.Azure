using Microsoft.Azure.WebJobs;

namespace Orders.WebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    public static class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            var config   = new JobHostConfiguration("DefaultEndpointsProtocol=https;AccountName=workshoptest;AccountKey=x+X7vjjElvWY9AN/qpUgm5d6ildh4P0tRPD/r4k+fkESnJ2zZu2Sr1lqv9/Y4ZSb+/nc1FzKnydCicrVMu5oAQ==;EndpointSuffix=core.windows.net");
            var host = new JobHost(config);
            //log.WriteLine("Hello");

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
