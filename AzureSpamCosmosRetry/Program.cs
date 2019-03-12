using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSpamCosmosRetry
{
    public class Program
    {

        private string _endpointUri = "https://yyy:443/";
        private string _accessKey = "xxx";
        private bool _useRetry = true;

        private readonly FooRepository _repo1;
        private readonly FooRepository _repo2;
        private readonly FooRepository _repo3;
        private readonly FooRepository _repo4;
        private readonly FooRepository _repo5;
        private string _oneDocId;

        public static void Main()
        {
            var prog = new Program();

            try
            {
                prog.InsertDocs().GetAwaiter().GetResult();
                prog.ReadDocs();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Console.WriteLine("Got exception: " + inner.Message);
            }

            prog.Cleanup().GetAwaiter().GetResult();

            Console.WriteLine("Finished! Press any key to exit ..");
            Console.ReadKey();
        }

        public Program()
        {
            // First, create a cosmos account and paste the endpointUri and accessKey here!
            _repo1 = new FooRepository(_endpointUri, _accessKey);
            _repo2 = new FooRepository(_endpointUri, _accessKey);
            _repo3 = new FooRepository(_endpointUri, _accessKey);
            _repo4 = new FooRepository(_endpointUri, _accessKey);
            _repo5 = new FooRepository(_endpointUri, _accessKey);
            Console.WriteLine("I will first make sure the db and collection exists, please wait ..");
            _repo1.EnsureDatabaseAndCollectionExists().GetAwaiter().GetResult();
        }

        public async Task InsertDocs()
        {
            _oneDocId = await _repo1.InsertOneDocGetId();
            
            Console.WriteLine("How many documents do you want to create?");
            int howMany;

            do
            {
                var userInput = Console.ReadLine();
                if (!int.TryParse(userInput, out howMany))
                {
                    Console.WriteLine("That's not a number! Try again: ");
                }

            } while (howMany == 0);

            Console.WriteLine("Creating "+howMany+" documents, please wait ..");

            await _repo1.InsertManyDocuments(howMany, _useRetry, new CancellationToken());

            Console.WriteLine("Created " + howMany + " documents.");
        }

        public void ReadDocs()
        {
            Console.WriteLine("How many times do you want to read a document?");
            int howMany;

            do
            {
                var userInputHowMany = Console.ReadLine();
                if (!int.TryParse(userInputHowMany, out howMany))
                {
                    Console.WriteLine("That's not a number! Try again:");
                }

            } while (howMany == 0);

            Console.WriteLine("Executing now! .. The console may appear to hang but it is working .. Retry: " + _useRetry);

            var cancelToken = new CancellationToken();
            
            var t1 = new Thread(async () => await _repo2.ReadSingleDocumentOverAndOver(_oneDocId, _useRetry, howMany));
            var t2 = new Thread(() => _repo1.SpamCosmosIndefinitely(howMany, _useRetry, cancelToken));
            var t3 = new Thread(async () => await _repo2.ReadSingleDocumentOverAndOver(_oneDocId, _useRetry, howMany));
            var t4 = new Thread(() => _repo4.SpamCosmosIndefinitely(howMany, _useRetry, cancelToken));
            var t5 = new Thread(async () => await _repo2.ReadSingleDocumentOverAndOver(_oneDocId, _useRetry, howMany));
            var t6 = new Thread(() => _repo5.SpamCosmosIndefinitely(howMany, _useRetry, cancelToken));
            var t7 = new Thread(async () => await _repo2.ReadSingleDocumentOverAndOver(_oneDocId, _useRetry, howMany));
            var t8 = new Thread(() => _repo5.SpamCosmosIndefinitely(howMany, _useRetry, cancelToken));
            var t9 = new Thread(async () => await _repo2.ReadSingleDocumentOverAndOver(_oneDocId, _useRetry, howMany));

            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t5.Start();
            t6.Start();
            t7.Start();
            t8.Start();
            t9.Start();

            t1.Join();
            t2.Join();
            t3.Join();
            t4.Join();
            t5.Join();
            t6.Join();
            t7.Join();
            t8.Join();
            t9.Join();

            _repo1.SpamCosmosIndefinitely(howMany, false, new CancellationToken());
            Console.WriteLine("Done reading!");
        }

        private async Task Cleanup()
        {
            Console.WriteLine("Do you want to delete the collection? [Y/N]");
            var userInput = Console.ReadLine();
            var shouldDelete = userInput == "y" || userInput == "Y";

            if (shouldDelete)
                await _repo1.DeleteCollection();
        }
    }
}