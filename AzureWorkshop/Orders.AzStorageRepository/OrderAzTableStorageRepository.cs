using System;
using System.Collections.Generic;
using System.Linq;
using Orders.Shared;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Orders.AzStorageRepository
{
    public class OrderAzTableStorageRepository : IOrderRepository
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _partitionKey;
        private readonly string _tableName;

        public OrderAzTableStorageRepository()
        {
            var accountName = "STORAGEACCOUNTNAME";
            var accountKey = "BLABLABLA";
            _tableName = "Orders";
            _partitionKey = "nya";

            var connString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

            _storageAccount = CloudStorageAccount.Parse(connString);
        }

        public OrderModel Get(Guid appSettingKey)
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_tableName);
            var retrieveOperation = TableOperation.Retrieve<OrderAzEntity<string>>(_partitionKey, appSettingKey.ToString());
            var retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null)
            {
                throw new NullReferenceException($"Nothing found in Azure Table!");
            }
            else
            {
                var convertedResult = (OrderAzEntity<string>)retrievedResult.Result;
                var retur = JsonConvert.DeserializeObject<OrderModel>(convertedResult.Value);
                return retur;
            }
        }

        public List<OrderModel> GetAll()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_tableName);
            var entities = table.ExecuteQuery(new TableQuery<OrderAzEntity<string>>()).ToList();
            var deserialized = entities.Select(p => JsonConvert.DeserializeObject<OrderModel>(p.Value)).ToList();
            return deserialized;
        }

        public void Add(OrderModel entity)
        {
            throw new NotImplementedException("Uups!");
        }
    }
}