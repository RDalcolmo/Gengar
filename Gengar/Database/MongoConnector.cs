﻿using Discord;
using Gengar.Models.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Gengar.Database
{
    public class MongoConnector
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoDatabase Database;

        public MongoConnector(IConfiguration configuration)
        {
            _configuration = configuration;

            try
            {
                var mcs = MongoClientSettings.FromUrl(new MongoUrl(_configuration["ConnectionString"]));

                IMongoClient client = client = new MongoClient(mcs);

                Database = client.GetDatabase("discordbots");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public IMongoCollection<Birthdays> Birthdays
        {
            get { return Database.GetCollection<Birthdays>("birthdays"); }
        }

    }
}
