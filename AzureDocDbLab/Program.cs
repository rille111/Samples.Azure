using System;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable UnusedParameter.Local
// ReSharper disable SuggestVarOrType_SimpleTypes
namespace ConsoleAppAzureDocDbLab
{
    internal class Program
    {
        private static readonly string EndpointUrl = "https://rille111.documents.azure.com:443/";
        private static readonly string AuthorizationKey = "SECRET!!!!";
        private static DocumentClient _client;
        private static Database _db;
        private static DocumentCollection _co;

        
        static void Main(string[] args)
        {
            Execute();
        }

        static void Execute()
        {
            // Create a new instance of the DocumentClient.
            _client = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey);
            Print("Creating db and collection");

            CreateOrReadDatabase("FamilyRegistry");

            CreateOrReadCollection("FamilyCollection");
            _client.DeleteDocumentCollectionAsync(_co.DocumentsLink).Wait(); // TODO: How can u more easily just delete all documents without destroying the collection?
            CreateOrReadCollection("FamilyCollection");

            Print("Deleting, then filling with families");

            var family1 = GenerateAndersenFamily();
            var family2 = GenerateWakefieldFamily();
            _client.CreateDocumentAsync(_co.DocumentsLink, family1).Wait();
            _client.CreateDocumentAsync(_co.DocumentsLink, family2).Wait();

            PrintAndWait("Now iterating");
            //IterateFamilies_1();
            IterateFamilies_UsingLinq();
            IterateFamilies_UsingLambda();
            IterateFamiliesAsDynamic_WithOneJoin();
            IterateFamiliesAsTyped_WithOneJoin();

            PrintAndWait("Done!");
        }

        private static object GenerateWakefieldFamily()
        {
            
            Family wakefieldFamily = new Family
            {
                Id = "WakefieldFamily",
                
                Parents = new Parent[] {
                    new Parent { FamilyName= "Wakefield", FirstName= "Robin" },
                    new Parent { FamilyName= "Miller", FirstName= "Ben" }
                },
                Children = new Child[] {
                    new Child {
                        FamilyName= "Merriam",
                        FirstName= "Jesse",
                        Gender= "female",
                        Grade= 8,
                        Pets= new Pet[] {
                            new Pet { GivenName= "Goofy" },
                            new Pet { GivenName= "Shadow" }
                        }
                    },
                    new Child {
                        FamilyName= "Miller",
                        FirstName= "Lisa",
                        Gender= "female",
                        Grade= 1
                    }
                },
                Address = new Address { State = "NY", County = "Manhattan", City = "NY" },
                IsRegistered = false
            };
            return wakefieldFamily;
        }

        private static void Print(string msg)
        {
            Console.WriteLine("{0}", msg);
        }

        private static void PrintAndWait(string msg)
        {
            Console.WriteLine("{0}. Press any key ...", msg);
            Console.ReadKey();
        }

        private static Family GenerateAndersenFamily()
        {
            // Create the Andersen family document.
            return new Family
            {
                Id = "AndersenFamily",
                LastName = "Andersen",
                Parents = new Parent[] {
                                            new Parent { FirstName = "Thomas" },
                                            new Parent { FirstName = "Mary Kay"}
                                        },
                Children = new Child[] {
                                            new Child {
                                                FirstName = "Henriette Thaulow",
                                                Gender = "female",
                                                Grade = 5,
                                                Pets = new Pet[] {
                                                    new Pet { GivenName = "Fluffy" }
                                                }
                                            }
                                        },
                Address = new Address { State = "WA", County = "King", City = "Seattle" },
                IsRegistered = true
            };

        }

        private static void CreateOrReadDatabase(string databaseName)
        {
            var resp = _client.CreateDatabaseQuery().Where(x => x.Id == databaseName).AsEnumerable().ToList();
            
            if (resp.Any())
            {
                _db = resp.First();
            }
            else
            {
                _db = _client.CreateDatabaseAsync(new Database { Id = databaseName }).Result;
            }
        }

        public static void CreateOrReadCollection(string collectionName)
        {
            if (_client.CreateDocumentCollectionQuery(_db.SelfLink).Where(c => c.Id == collectionName).ToArray().Any())
            {
                _co = _client.CreateDocumentCollectionQuery(_db.SelfLink).Where(c => c.Id == collectionName).ToArray().First();
            }
            else
            {
                _co = _client.CreateDocumentCollectionAsync(_db.SelfLink, new DocumentCollection { Id = collectionName }).Result;
            }

        }

        private static void IterateFamilies_UsingSQL()
        {
            // Query the documents using DocumentDB SQL for the Andersen family.
            var families = _client.CreateDocumentQuery(_co.DocumentsLink,
                "SELECT * " +
                "FROM Families f " +
                "WHERE f.id = \"AndersenFamily\"");

            foreach (var family in families)
            {
                Console.WriteLine("\tRead {0} from SQL", family);
            }
        }

        private static void IterateFamilies_UsingLinq()
        {
            // Query the documents using LINQ for the Andersen family.
            var families =
                from f in _client.CreateDocumentQuery(_co.DocumentsLink)
                where f.Id == "AndersenFamily"
                select f;

            foreach (var family in families)
            {
                Console.WriteLine("\tRead {0} from LINQ", family);
            }
        }

        private static void IterateFamilies_UsingLambda()
        {
            // Query the documents using LINQ lambdas for the Andersen family.
            var families = _client.CreateDocumentQuery(_co.DocumentsLink)
                .Where(f => f.Id == "AndersenFamily")
                .Select(f => f);

            foreach (var family in families)
            {
                Console.WriteLine("\tRead {0} from LINQ query", family);
            }
        }

        private static void IterateFamiliesAsDynamic_WithOneJoin()
        {
            // Query the documents using DocumentSQL with one join.
            var items = _client.CreateDocumentQuery<dynamic>(_co.DocumentsLink,
                "SELECT f.id, c.FirstName AS child " +
                "FROM Families f " +
                "JOIN c IN f.Children");

            foreach (var item in items.ToList())
            {
                Console.WriteLine(item);
            }
        }

        private static void IterateFamiliesAsTyped_WithOneJoin()
        {
            // Query the documents using LINQ with one join.
            var items = _client.CreateDocumentQuery<Family>(_co.DocumentsLink)
                .SelectMany(family => family.Children
                    .Select(children => new
                    {
                        family = family.Id,
                        child = children.FirstName
                    }));

            foreach (var item in items.ToList())
            {
                Console.WriteLine(item);
            }
        }

    }

    internal class Pet
    {
        public string GivenName { get; set; }
    }

    internal class Child
    {
        public string FirstName { get; set; }
        public string Gender { get; set; }
        public int Grade { get; set; }
        public Pet[] Pets { get; set; }
        public string FamilyName { get; set; }
    }

    internal class Parent

    {
        public string FamilyName { get; set; }
        public string FirstName { get; set; }
    }

    internal class Family

    {
        public Address Address { get; internal set; }
        public string Id { get; set; }
        public string LastName { get; set; }
        public Parent[] Parents { get; set; }
        public Child[] Children { get; set; }
        public bool IsRegistered { get; set; }
    }

    internal class Address
    {
        public string City { get; set; }
        public string County { get; set; }
        public string State { get; set; }
    }
}
