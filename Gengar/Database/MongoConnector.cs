using DnsClient.Internal;
using Gengar.Models.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Gengar.Database;

public class MongoConnector
{
    private readonly IConfiguration _configuration;
    private readonly IMongoDatabase Database;
    private readonly ILogger<MongoConnector> _logger;

    public MongoConnector(IConfiguration configuration, ILogger<MongoConnector> logger)
    {
        _configuration = configuration;
        _logger = logger;

        try
        {
            var mcs = MongoClientSettings.FromUrl(new MongoUrl(_configuration["ConnectionString"]));

            MongoClient client = new(mcs);

            Database = client.GetDatabase("discordbots");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public IMongoCollection<Birthdays> Birthdays
    {
        get { return Database.GetCollection<Birthdays>("birthdays"); }
    }
}
