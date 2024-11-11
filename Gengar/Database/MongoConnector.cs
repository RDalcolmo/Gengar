using DnsClient.Internal;
using Gengar.Models.Mongo;
using Gengar.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Gengar.Database;

public class MongoConnector
{
    private readonly IOptions<MongoDbOptions> _options;
    private readonly IMongoDatabase Database;
    private readonly ILogger<MongoConnector> _logger;

    public MongoConnector(IOptions<MongoDbOptions> options, ILogger<MongoConnector> logger)
    {
        _options = options;
        _logger = logger;

        try
        {
            var mcs = MongoClientSettings.FromUrl(new MongoUrl(_options.Value.ConnectionString));

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
