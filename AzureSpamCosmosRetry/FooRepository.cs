using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSpamCosmosRetry
{
    public class FooRepository
    {
        private readonly string _databaseId;
        private readonly string _collectionId;
        private readonly DocumentClient _client;
        private readonly Uri _collectionUri;

        public FooRepository(string endpointUri, string accessKey, string databaseId = "MyDatabase", string collectionId = "MyFooCollection")
        {
            _databaseId = databaseId;
            _collectionId = collectionId;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            var connPolicy = new ConnectionPolicy
            {
                RetryOptions = new RetryOptions
                {
                    MaxRetryAttemptsOnThrottledRequests = 1,
                    MaxRetryWaitTimeInSeconds = 1
                }
            };

            this._client = new DocumentClient(new Uri(endpointUri), accessKey, connPolicy);
        }

        public async Task EnsureDatabaseAndCollectionExists()
        {
            await this._client.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseId });
            await this._client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_databaseId)
                , new DocumentCollection { Id = _collectionId });
        }

        public async Task<string> InsertOneDocGetId()
        {
            var doc = await _client.CreateDocumentAsync(_collectionUri, new Foo());
            return doc.Resource.Id;
        }

        public async Task InsertManyDocuments(int howMany, bool useRetry, CancellationToken cancelToken)
        {
            for (var i = 0; i < howMany; i++)
            {
                if (useRetry)
                {
                    await ExecuteWithRetriesAsync(async () => await _client.CreateDocumentAsync(_collectionUri, new Foo(), cancellationToken: cancelToken));
                }
                else
                {
                    await _client.CreateDocumentAsync(_collectionUri, new Foo(), cancellationToken: cancelToken);
                }
                
            }
        }

        public async Task ReadSingleDocumentOverAndOver(string docId, bool useRetry, int howManyTimes)
        {

            var docUri = UriFactory.CreateDocumentUri(_databaseId, _collectionId, docId);

            for (int i = 0; i < howManyTimes; i++)
            {
                if (useRetry)
                {
                    var doc = await ExecuteWithRetriesAsync(async () => await _client.ReadDocumentAsync<Foo>(docUri));
                }
                else
                {
                    var doc = await _client.ReadDocumentAsync<Foo>(docUri);
                }
            }
        }

        public void SpamCosmosIndefinitely(int howManyTimes, bool useRetryAfter, CancellationToken cancelToken)
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };
            try
            {
                while(true)
                {
                    var query = _client.CreateDocumentQuery<Foo>(_collectionUri
                        , "SELECT * FROM c"
                        , queryOptions
                    );
                    var x = query.ToList();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        /// <summary>
        /// Execute the function with retries on throttle
        /// </summary>
        public async Task<T> ExecuteWithRetriesAsync<T>(Func<Task<T>> function)
        {
            // ReSharper disable PossibleInvalidOperationException
            while (true)
            {
                TimeSpan sleepTime;
                try
                {
                    return await function();
                }
                catch (DocumentClientException de)
                {
                    if ((int)de.StatusCode != 429)
                    {
                        throw;
                    }
                    sleepTime = de.RetryAfter;
                }
                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is DocumentClientException))
                    {
                        throw;
                    }

                    var de = (DocumentClientException)ae.InnerException;
                    if ((int)de.StatusCode != 429)
                    {
                        throw;
                    }
                    sleepTime = de.RetryAfter;
                }
                Console.WriteLine("Backoff! Sleep for total seconds: " + sleepTime.TotalSeconds);
                await Task.Delay(sleepTime);
            }
        }

        public async Task DeleteCollection()
        {

            await _client.DeleteDocumentCollectionAsync(_collectionUri);
        }
    }
}
