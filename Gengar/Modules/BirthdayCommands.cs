using System;
using System.Linq;
using System.Threading.Tasks;
using Gengar.Models;
using Discord.Commands;
using Discord;
using Discord.Addons.Interactive;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.VisualBasic;

namespace Gengar.Modules
{
	[Group("bday")]
	public class BirthdayCommands : ModuleBase<SocketCommandContext>
	{
		[Command("next", RunMode = RunMode.Async), Summary("Checks if there are any birthdays within 14 days.")]
		public async Task CheckBirthdays()
		{
            using var _dbContext = new GengarContext();
            var nextBday = await _dbContext.TblBirthdays.FromSqlRaw("select userid, birthday, comments from tblbirthdays where to_char(birthday,'ddd')::int-to_char(now(),'DDD')::int between 0 and 15;").ToListAsync().ConfigureAwait(false);

            if (Context.Guild != null)
            {
                foreach (var user in nextBday.ToList())
                {

                    if (Context.Guild.GetUser((ulong)user.Userid) == null)
                    {
                        nextBday.Remove(user);
                    }

                }
            }

            if (nextBday.Count > 0)
            {
                string _content;
                if (nextBday.Count == 1)
                    _content = "There is 1 upcoming birthday!";
                else
                    _content = $"There are {nextBday.Count} upcoming birthdays!";

                _content += $"\nThe next person's birthday is:";
                foreach (var person in nextBday.OrderBy(m => m.Birthday.Month).ThenBy(d => d.Birthday.Day))
                {
                    _content += $"\n<@{person.Userid}> on {person.Birthday:MMMM dd}!";
                }
                await ReplyAsync(_content).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("There are no birthdays in the next 14 days!").ConfigureAwait(false);
            }
        }

		[Command("month")]
		[Alias("m")]
		[RequireContext(ContextType.DM)]
		public async Task BirthdayInMonth([Remainder] string month)
		{

			string[] formats = { "M", "MM", "MMM", "MMMM" };
            if (month.Length == 1)
                month = "0" + month;
            DateTime.TryParseExact(month, formats, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime parsedMonth);
            using var _dbContext = new GengarContext();
            //var nextBday = db.TblBirthdays.Where(id => id.Birthday.Value.Month == parsedMonth.Month).OrderBy(bday => bday.Birthday.Value.Day).ToList();
            var nextBday = await _dbContext.TblBirthdays.FromSqlRaw($"SELECT userid, birthday, comments FROM tblbirthdays WHERE EXTRACT(MONTH FROM birthday) = {parsedMonth.Month} ORDER BY birthday").ToListAsync().ConfigureAwait(false);

            if (nextBday.Count > 0)
            {
                string _content;
                if (nextBday.Count == 1)
                    _content = $"There is 1 birthday in the month of {parsedMonth:MMMM}!";
                else
                    _content = $"There are {nextBday.Count} birthdays in the month of {parsedMonth:MMMM}!";

                _content += $"\nBirthdays found in this month are:";
                foreach (var person in nextBday)
                {
                    _content += $"\n<@{person.Userid}> on {person.Birthday:MMMM dd}!";
                }
                await ReplyAsync(_content).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("There are no birthdays in this month!").ConfigureAwait(false);
            }
        }

		[Command("when")]
		[RequireContext(ContextType.DM)]
		public async Task WhenIsBirthday([Remainder] string UserID)
		{
            using var _dbContext = new GengarContext();
            //if (_dbContext.TblBirthdays.AsQueryable().Where(id => id.Userid == Convert.ToInt64(UserID)).Any())
            var person = await _dbContext.TblBirthdays.FindAsync(Convert.ToInt64(UserID)).ConfigureAwait(false);

            if (person != null)
            {
                await ReplyAsync($"<@{person.Userid}>'s Birthday is on {person.Birthday:MMMM dd}").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("This person does not have a birthday registered in our database!").ConfigureAwait(false);
            }
        }

		[Command("remove")]
		[RequireContext(ContextType.DM)]
		[RequireOwner]
		public async Task RemoveUser([Remainder] string UserID)
		{
            using var _dbContext = new GengarContext();
            //if (_dbContext.TblBirthdays.AsQueryable().Where(id => id.Userid == Convert.ToInt64(UserID)).Any())
            var person = await _dbContext.TblBirthdays.FindAsync(Convert.ToInt64(UserID)).ConfigureAwait(false);

            if (person != null)
            {
                _dbContext.Remove(person);
                await ReplyAsync($"Removed user <@{person.Userid}>'s Birthday.").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("This person does not have a birthday registered in our database!").ConfigureAwait(false);
            }
            await _dbContext.SaveChangesAsync();
        }

		[Command("bcast")]
		[RequireOwner]
		public async Task Broadcast()
		{
			Console.WriteLine($"Broadcasting today's birthdays: {DateTime.Today.ToLongDateString()}");
            using var _dbContext = new GengarContext();
            var Guild = Context.Guild;

            if (Guild == null)
                return;

            Console.WriteLine($"Detected Guild: {Guild.Name}");
            var Channel = Guild.GetTextChannel(Convert.ToUInt64(Startup.Configuration["DiscordChannel"]));

            if (Channel == null)
                return;

            Console.WriteLine($"Detected Broadcast Channel: {Channel.Name}");
            var birthday = _dbContext.TblBirthdays.AsNoTracking().Where(d => d.Birthday.Month == DateTime.Now.Month && d.Birthday.Day == DateTime.Now.Day).ToList();

            Console.WriteLine($"Total birthdays today: {birthday.Count}");

            foreach (var user in birthday.ToList())
            {
                var userInGuild = Guild.GetUser((ulong)user.Userid);

                if (userInGuild == null)
                {
                    birthday.Remove(user);
                }
            }

            if (birthday.Count > 0)
            {
                string _content;
                if (birthday.Count == 1)
                    _content = "There is 1 birthday today!";
                else
                    _content = $"There are {birthday.Count} birthdays today!";

                foreach (var person in birthday)
                {
                    _content += $"\nIt's <@{person.Userid}> birthday today!! Happy birthday!";
                }

                await Channel.SendMessageAsync(_content).ConfigureAwait(false);
            }
        }
	}


	[Group("bcast")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	[RequireContext(ContextType.Guild)]
	public class RegistrationModule : ModuleBase<SocketCommandContext>
	{

		[Command("set"), Summary("Sets a new channel to broadcast birthdays in.")]
		public async Task SetToChannel()
		{
			if (Convert.ToUInt64(Startup.Configuration["DicordChannel"]) != Context.Channel.Id)
			{
				Startup.Configuration["DicordChannel"] = Context.Channel.Id.ToString();
				await ReplyAsync("Broadcasting channel has been changed. Birthday messages will now be posted here.").ConfigureAwait(false);
			}
			else
			{
				await ReplyAsync("Messages are already being posted in the current channel.").ConfigureAwait(false);
			}
		}

		[Command("remove"), Summary("Stops broadcasting birthdays to the server.")]
		public async Task RemoveFromChannel()
		{
			Startup.Configuration["DiscordChannel"] = "";
			await ReplyAsync("The current broadcasting channel has been removed.").ConfigureAwait(false);
		}
	}

	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		[Command("help")]
		public async Task Help()
		{
			var builder = new EmbedBuilder()
								.WithTitle("List of Commands:")
								.WithColor(new Color(0xBC00F3))
								.AddField("Admin Commands", "`!bcast set` - Assigns the current channel to the broadcasting list.\n`!bcast remove` - Removes the bot from broadcasting to the guild.")
								.AddField("User Commands", "`!bday next` - Broadcast a list of birthdays within the next 14 days.\n`!bday m MONTH` - Broadcast a list of birthdays in the specified month. Valid formats: 12, Dec, December.\n");
			var embed = builder.Build();
			await Context.Channel.SendMessageAsync(null, embed: embed).ConfigureAwait(false);
		}
	}

	public class MiscModule : ModuleBase<SocketCommandContext>
	{
		[Command("cookie")]
		public async Task Cookie()
		{
			await ReplyAsync($"Have a cookie!! <:skyerafLove:577608611099050025>").ConfigureAwait(false);
		}

		[Command("cake")]
		public async Task Cake()
		{
			await ReplyAsync($"Sorry, cakes are only for those who have a birthday").ConfigureAwait(false);
		}

		[Command("time")]
		public async Task GetTime()
		{
			await ReplyAsync($"Current time is: {DateTime.Now.ToLocalTime()}").ConfigureAwait(false);
		}
		//[Command("shoutout", RunMode = RunMode.Async)]
		//public async Task Shoutout()
		//{
		//	await ReplyAsync("Who would you like to give a shoutout to?");
		//	var response = await NextMessageAsync();

		//	if (response != null)
		//	{
		//		await ReplyAsync($"Shoutouts to {response.Content}");
		//	}
		//	else
		//		await ReplyAsync("You took too long!");
		//}

		//[Command("add", RunMode = RunMode.Async)]
		//public async Task Add()
		//{
		//	await ReplyAsync("What's the first number");

		//	var response = await NextMessageAsync();

		//	if (response != null)
		//	{
		//		await ReplyAsync($"What's the second number?");
		//		var response2 = await NextMessageAsync();

		//		if (response2 != null)
		//		{
		//			int one;
		//			int two;
		//			Int32.TryParse(response.Content, out one);
		//			Int32.TryParse(response2.Content, out two);
		//			await ReplyAsync($"The numbers added are {one + two}");

		//		}
		//	}
		//}
	}
}
