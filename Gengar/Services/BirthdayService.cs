using Gengar.Database;
using Gengar.Models.Mongo;
using MongoDB.Driver;

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
                Builders<Birthdays>.Filter.Gte(x => x.Birthday, today.Date),
                Builders<Birthdays>.Filter.Lte(x => x.Birthday, next14days.Date)
            );

            return (await _dbContext.Birthdays.FindAsync(filter)).ToList();
        }

        public async Task<List<Birthdays>> GetAllUsers()
        {
            return (await _dbContext.Birthdays.FindAsync(Builders<Birthdays>.Filter.Empty)).ToList();
        }

        public async Task<List<Birthdays>> GetTodaysBirthdays()
        {
            var filter = Builders<Birthdays>.Filter.And(
                Builders<Birthdays>.Filter.Eq(x => x.Birthday.Month, DateTime.Today.Month),
                Builders<Birthdays>.Filter.Eq(x => x.Birthday.Day, DateTime.Today.Day));

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
                Builders<Birthdays>.Update.Set(x => x.Birthday, user.Birthday)
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
