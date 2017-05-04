using Microsoft.WindowsAzure.Storage.Table;

namespace Orders.AzStorageRepository
{
    /// <summary>
    /// This is a class used for integration with azure table storage.
    /// We must inherit from TableEntity to be able to cast to this class when we interact with azure table storage.
    /// </summary>
    internal class OrderAzEntity<T> : TableEntity
    {
        public OrderAzEntity() { }

        public OrderAzEntity(string partitionKey, string settingKey, T value)
        {
            PartitionKey = partitionKey;
            RowKey = settingKey;

            Value = value;
        }

        /// <summary>
        /// Right now we use the 'Value' string field from Table, to actually persist ENTIRE objects (as serialized Json), but otherwise we could just add those fields inside this class and they would get populated auto.
        /// So we kinda "emulate" a schemaless docdb persistence mechacnism inside Azure Tables, hehe.
        /// </summary>
        public T Value { get; set; }
    }
}