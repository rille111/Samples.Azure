using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace GroupTableStorage.Test
{
	[TestClass]
	public class Generator
	{		
		[TestMethod]
		public void GenerateData()
		{
			var range = Enumerable.Range(0, 100000);

			using (var writer = new StreamWriter("C:\\temp\\testdata.json"))
			{
				var list = range.Select(i => GenerateEntity());

				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(writer, list);				
			}				
		}

		private static Entity GenerateEntity()
		{
			return new Entity()
			{
				Id = ComputeOriginalId(Guid.NewGuid().ToString()),
				Value = ComputeOriginalId(Guid.NewGuid().ToString())
			};
		}

		private static string ComputeOriginalId(string customerId)
		{			
			// salt is a hardcoded secret
			const string salt = "667C6270-5521-48CD-8D46-7123777C74DC";
			var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + customerId));

			// Base64encode for non binary handling and convert to url friendly version.
			return Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-');
		}		
	}
}
