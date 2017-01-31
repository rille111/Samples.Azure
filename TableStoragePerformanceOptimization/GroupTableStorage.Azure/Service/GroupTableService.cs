using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GroupTableStorage.Azure.Service
{
    public class GroupTableService
    {
	    private readonly Dictionary<string, List<GroupEntity>> _updates = new Dictionary<string, List<GroupEntity>>();

	    private static CloudTable _table;

		private static readonly string[] PartitionKeys = {
			"A", "Q", "g", "w",
			"B", "R", "h", "x",
			"C", "S", "i", "y",
			"D", "T", "j", "z",
			"E", "U", "k", "0",
			"F", "V", "l", "1",
			"G", "W", "m", "2",
			"H", "X", "n", "3",
			"I", "Y", "o", "4",
			"J", "Z", "p", "5",
			"K", "a", "q", "6",
			"L", "b", "r", "7",
			"M", "c", "s", "8",
			"N", "d", "t", "9",
			"O", "e", "u", "_",
			"P", "f", "v", "-"};

	    public GroupTableService(string connectionString)
	    {
		    foreach (var key in PartitionKeys)
		    {
			    _updates[key] = new List<GroupEntity>();
		    }

			ServicePointManager.DefaultConnectionLimit = 100;
			var storageAccount = CloudStorageAccount.Parse(connectionString);

			var endpoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
			endpoint.Expect100Continue = false;
			endpoint.UseNagleAlgorithm = false;

			var tableClient = storageAccount.CreateCloudTableClient();

			_table = tableClient.GetTableReference($"test{Environment.TickCount.ToString().Replace("-", "X")}");
			_table.CreateIfNotExists();
		}

		public void Add(string id, string value)
		{
			var key = id[0].ToString();

			_updates[key].Add(new GroupEntity(key, id)
			{
				Value = value
			});
	    }
	    public async Task Exec()
	    {		    
			var tasks = _updates.Keys.Select(key => InsertOrReplaceProductsIntoTable(_updates[key]));
			await Task.WhenAll(tasks);
	    }
		private async Task InsertOrReplaceProductsIntoTable(List<GroupEntity> updates)
		{
			var rowOffset = 0;
			while (rowOffset < updates.Count)
			{
				var batch = new TableBatchOperation();
				var rows = updates.Skip(rowOffset).Take(100).ToList();

				foreach (var row in rows)
				{					
					batch.InsertOrReplace(row);
				}

				await _table.ExecuteBatchAsync(batch);

				rowOffset += rows.Count;
			}
		}
	}
}
