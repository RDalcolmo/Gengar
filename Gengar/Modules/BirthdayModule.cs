using Discord;
using Discord.Interactions;
using Gengar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace Gengar.Modules
{
    [Group("bday", "Birthday commands")]
    public class BirthdayModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GengarContext _dbContext;
        private readonly IConfiguration _configuration;

        public BirthdayModule(GengarContext dbContext, IConfiguration configuration)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [SlashCommand("next", "Checks if there are any birthdays within 14 days")]
        public async Task CheckBirthdays()
        {
            var nextBday = _dbContext.TblBirthdays.FromSqlRaw("select userid, birthday, comments from tblbirthdays where to_char(birthday,'ddd')::int-to_char(now(),'DDD')::int between 0 and 15;").AsNoTracking().ToList();

            if (Context.Guild != null)
            {
                foreach (var user in nextBday.ToList())
                {

                    if (Context.Guild.GetUser(user.Userid) == null)
                    {
                        nextBday.Remove(user);
                    }
                }
            }

            var numberOfBirthdays = nextBday.Count;
            string _content;

            if (numberOfBirthdays == 0)
            {
                _content = "There are no birthdays in the next 14 days!";
            }
            else
            {
                _content = $"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} upcoming birthdays" : "is 1 upcoming birthday")}!"
                            + $"\nThe next person's birthday is:";

                foreach (var person in nextBday.OrderBy(m => m.Birthday.Month).ThenBy(d => d.Birthday.Day))
                {
                    _content += $"\n<@{person.Userid}> on {person.Birthday:MMMM dd}!";
                }
            }
            
            await RespondAsync(_content, ephemeral: true);
        }

        [SlashCommand("month", "Gets a list of all birthdays in the chosen month")]
        public async Task BirthdayInMonth([Summary(description: "Choose the month")] Month month)
        {
            var nextBday = _dbContext.TblBirthdays.AsNoTracking().Where(id => id.Birthday.Month == (int)month).OrderBy(bday => bday.Birthday.Day).ToList();

            var numberOfBirthdays = nextBday.Count;

            string _content;

            if (numberOfBirthdays == 0)
            {
                _content = "There are no birthdays in this month!";
            }
            else
            {
                _content = $"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} birthdays" : "is 1 birthday")} in the month of {month}!"
                            + $"\nBirthdays found in this month are:";

                foreach (var person in nextBday.OrderBy(m => m.Birthday.Month).ThenBy(d => d.Birthday.Day))
                {
                    _content += $"\n<@{person.Userid}> on {person.Birthday:MMMM dd}!";
                }
            }
            
            await RespondAsync(_content, ephemeral: true);
        }

        [SlashCommand("when", "Gets the birthday date of the given discord user ID")]
        public async Task WhenIsBirthday([Summary(description: "Discord user ID")] ulong userid)
        {
            var person = await _dbContext.TblBirthdays.AsNoTracking().Where(u => u.Userid == userid).FirstOrDefaultAsync();
            string _content;
           
            if (person == null)
                _content = "This person does not have a birthday registered in our database!";
            else
                _content = $"<@{person.Userid}>'s Birthday is on {person.Birthday:MMMM dd}";

            await RespondAsync(_content, ephemeral: true);
        }

        [SlashCommand("remove", "Removes a birthday from the database")]
        [RequireOwner]
        public async Task RemoveUser([Summary(description: "Discord user ID")] ulong userid)
        {
            var person = await _dbContext.TblBirthdays.FindAsync(userid);

            string _content;

            if (person == null)
                _content = "This person does not have a birthday registered in our database!";
            else
            {
                _dbContext.Remove(person);
                await _dbContext.SaveChangesAsync();
                _content = $"Removed user <@{person.Userid}>'s Birthday.";
            }

            await RespondAsync(_content, ephemeral: true);
        }

        [SlashCommand("bcast", "Broadcasts the birthday messages to the channel set")]
        [RequireOwner]
        public async Task Broadcast()
        {
            Console.WriteLine($"Broadcasting today's birthdays: {DateTime.Today.ToLongDateString()}");

            var Guild = Context.Guild;

            if (Guild == null)
                return;

            Console.WriteLine($"Detected Guild: {Guild.Name}");
            var Channel = Guild.GetTextChannel(Convert.ToUInt64(_configuration["DiscordChannel"]));

            if (Channel == null)
                return;

            Console.WriteLine($"Detected Broadcast Channel: {Channel.Name}");
            var birthday = _dbContext.TblBirthdays.AsNoTracking().Where(d => d.Birthday.Month == DateTime.Now.Month && d.Birthday.Day == DateTime.Now.Day).ToList();

            Console.WriteLine($"Total birthdays today: {birthday.Count}");

            foreach (var user in birthday.ToList())
            {
                var userInGuild = Guild.GetUser(user.Userid);

                if (userInGuild == null)
                {
                    birthday.Remove(user);
                }
            }

            var numberOfBirthdays = birthday.Count;

            if (numberOfBirthdays == 0)
            {
                await RespondAsync("There are no birthdays today", ephemeral: true);
                return;
            }
                
            string _content = $"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} birthdays" : "is 1 birthday")} today!";
            foreach (var person in birthday)
            {
                _content += $"\nIt's <@{person.Userid}> birthday today!! Happy birthday!";
            }

            await Channel.SendMessageAsync(_content);    
        }
    }

    [Group("bcast", "Group owner commands")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RequireContext(ContextType.Guild)]
    public class RegistrationModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IConfiguration _configuration;

        public RegistrationModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [SlashCommand("set", "Sets a new channel to broadcast birthdays in")]
        public async Task SetToChannel()
        {
            string _content;
            if (Convert.ToUInt64(_configuration["DicordChannel"]) != Context.Channel.Id)
            {
                _configuration["DicordChannel"] = Context.Channel.Id.ToString();
                _content = "Broadcasting channel has been changed. Birthday messages will now be posted here.";
            }
            else
            {
                _content = "Messages are already being posted in the current channel";
            }

            await RespondAsync(_content, ephemeral: true);
        }

        [SlashCommand("remove", "Stops broadcasting birthdays to the server")]
        public async Task RemoveFromChannel()
        {
            _configuration["DiscordChannel"] = "";
            await RespondAsync("The current broadcasting channel has been removed.", ephemeral: true);
        }
    }
}
