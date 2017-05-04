using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Documents.Client;
using Orders.Shared;

namespace Orders.AzDocDbRepository
{
    public class OrderAzDocDbRepository : IOrderRepository
    {
        private readonly DocumentClient _client;
        private readonly Uri _collectionUri;
        private readonly string _dbId;
        private readonly string _collectionId;

        public OrderAzDocDbRepository()
        {
            var endpointUri = new Uri("https://SOMETHING.documents.azure.com:443");
            var authKey = "dkfjhsdkjfhdskjf";

            _dbId = "SOMEDATABASE";
            _collectionId = "Orders";
            _client = new DocumentClient(endpointUri, authKey);
            _collectionUri = UriFactory.CreateDocumentCollectionUri(_dbId, _collectionId);
        }


        public OrderModel Get(Guid identifier)
        {
            var docUri = UriFactory.CreateDocumentUri(_dbId, _collectionId, identifier.ToString());
            var response = _client.ReadDocumentAsync(docUri).ConfigureAwait(false).GetAwaiter().GetResult();
            return (OrderModel)(dynamic)response.Resource;
        }

        public List<OrderModel> GetAll()
        {
            return _client.CreateDocumentQuery<OrderModel>(_collectionUri).ToList();
        }

        public void Add(OrderModel entity)
        {
            var result = _client.CreateDocumentAsync(_collectionUri, entity).ConfigureAwait(false).GetAwaiter().GetResult();
            if (result.StatusCode != HttpStatusCode.Created)
            {
                throw new ApplicationException($"Couldn't create {entity.GetType().Name}");
            }
        }
    }
}
