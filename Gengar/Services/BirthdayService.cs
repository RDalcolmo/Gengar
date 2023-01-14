using Discord;
using Gengar.Database;
using Gengar.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gengar.Services
{
    public class BirthdayService
    {
        private readonly MongoConnector _dbContext;

        public BirthdayService(MongoConnector dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Birthdays> GetUserById(ulong userId) =>
            (await _dbContext.Birthdays.FindAsync(x => x._id == userId)).FirstOrDefault();

        public async Task<List<Birthdays>> GetUsersNextTwoWeeks()
        {
            var today = DateTime.Today;
            var next14days = today.AddDays(14);

            var filter = Builders<Birthdays>.Filter.And(
                Builders<Birthdays>.Filter.Gte(x => x.BirthdayDate, today.Date),
                Builders<Birthdays>.Filter.Lte(x => x.BirthdayDate, next14days.Date)
            );

            return (await _dbContext.Birthdays.FindAsync(filter)).ToList();
        }

        public async Task<List<Birthdays>> GetUsersByMonth(int month)
        {
            var filter = Builders<Birthdays>.Filter.Eq(x => x.BirthdayDate.Month, month);

            return (await _dbContext.Birthdays.FindAsync(filter)).ToList();
        }

        public async Task<List<Birthdays>> GetTodaysBirthdays()
        {
            var filter = Builders<Birthdays>.Filter.And(
                Builders<Birthdays>.Filter.Eq(x => x.BirthdayDate.Month, DateTime.Today.Month),
                Builders<Birthdays>.Filter.Eq(x => x.BirthdayDate.Day, DateTime.Today.Day));

            return (await _dbContext.Birthdays.FindAsync(filter)).ToList();
        }

        public async Task Create(Birthdays user)
        {
            await _dbContext.Birthdays.InsertOneAsync(user);
        }

        public async Task Patch(Birthdays user)
        {
            var filter = Builders<Birthdays>.Filter.Eq(x => x._id, user._id);
            List<UpdateDefinition<Birthdays>> updateList = new()
            {
                Builders<Birthdays>.Update.Set(x => x.BirthdayDate, user.BirthdayDate)
            };

            if (updateList.Any())
            {
                UpdateDefinition<Birthdays> allUpdates = Builders<Birthdays>.Update.Combine(updateList);

                await _dbContext.Birthdays.UpdateOneAsync(filter, allUpdates, new UpdateOptions { IsUpsert = true });
            }
        }

        public async Task Remove(ulong userId)
        {
            var filter = Builders<Birthdays>.Filter.Eq(x => x._id, userId);
            await _dbContext.Birthdays.DeleteOneAsync(filter);
        }
    }
}
