using Microsoft.WindowsAzure.Storage.Table;

namespace GroupTableStorage.Azure.Service
{
	public class GroupEntity : TableEntity
	{
		public string Value { get; set; }

		public GroupEntity(string group, string id)
		{
			PartitionKey = group;
			RowKey = id;
		}
	}
}
