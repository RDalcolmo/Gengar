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
        private ILogger<MongoConnector> _Logger;

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
                        _Logger.LogCritical(ex, "Error connecting to database");
                        throw;
                    }
                }

                return _Database;
            }
        }

        public MongoConnector(IConfiguration configuration, ILogger<MongoConnector> logger)
        {
            _configuration = configuration;
            _Logger = logger;
        }

        public IMongoCollection<Birthdays> Birthdays
        {
            get { return Database.GetCollection<Birthdays>("birthdays"); }
        }

    }
}
