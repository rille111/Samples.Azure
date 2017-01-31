using GroupTableStorage.Azure.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GroupTableStorage
{
	class Program
	{
		class Entity
		{
			public string Id { get; set; }
			public string Value { get; set; }
		}

		static void Main(string[] args)
		{
			Program p = new Program();
			p.RunAsync().Wait();
		}

		async Task RunAsync()
		{
			var service = new GroupTableService(Byt ut mot connectionsträng till storage);
			var start = Environment.TickCount;

			Console.Write($"Starting {start}");

			using (var reader = new StreamReader("C:\\temp\\testdata.json"))
			{
				JsonSerializer serializer = new JsonSerializer();
				var entities = serializer.Deserialize<List<Entity>>(new JsonTextReader(reader));
				foreach (var entity in entities)
				{
					service.Add(entity.Id, entity.Value);
				}
				await service.Exec();
			}

			var end = Environment.TickCount;
			Console.Write($"Ending {end - start}");
			Console.ReadLine();
		}
	}
}
