using Discord;
using Gengar.Models.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Gengar.Database
{
    public class MongoConnector
    {
        private IConfiguration _configuration;

        private IMongoDatabase _Database;
        private IMongoDatabase Database
        {
            get
            {
                if (_Database == null)
                {
                    try
                    {
                        var mcs = MongoClientSettings.FromUrl(new MongoUrl(_configuration["ConnectionString"]));

                        IMongoClient client = client = new MongoClient(mcs);

                        _Database = client.GetDatabase("discordbots");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }

                return _Database;
            }
        }

        public MongoConnector(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IMongoCollection<Birthdays> Birthdays
        {
            get { return Database.GetCollection<Birthdays>("birthdays"); }
        }

    }
}
