using Discord.WebSocket;
using Gengar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gengar.Services
{
    public class BroadcastService : IHostedService
    {
		private static Timer _timer;
		private readonly DiscordSocketClient _discord;

		public BroadcastService(DiscordSocketClient discord)
		{
			_discord = discord;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Console.WriteLine("Starting Broadcast service.");
			TimeSpan interval = TimeSpan.FromHours(24);
			//calculate time to run the first time & delay to set the timer
			//DateTime.Today gives time of midnight 00.00
			var nextRunTime = DateTime.Today.AddDays(1).AddHours(10);
			var curTime = DateTime.Now;
			var firstInterval = nextRunTime.Subtract(curTime);

			Action action = () =>
			{
				Console.WriteLine("Action started.");
				var t1 = Task.Delay(firstInterval);
				t1.Wait();
				// Schedule it to be called every 24 hours
				// timer repeates call to RemoveScheduledAccounts every 24 hours.
				_timer = new Timer(BroadcastBirthday, null, TimeSpan.Zero, interval);
			};
			Console.WriteLine("Starting action.");
			// no need to await this call here because this task is scheduled to run much much later.
			Task.Run(action);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
            Console.WriteLine("Disposing timer.");
			_timer?.Dispose();
			return Task.CompletedTask;
		}

		public List<Tblguilds> GetGuildInformation()
		{
			using (var _dbContext = new GengarContext())
			{
				return _dbContext.TblGuilds.ToList();
			}
		}

		public async void BroadcastBirthday(object state)
		{
            Console.WriteLine($"Broadcasting today's birthdays: {DateTime.Today.ToLongDateString()}");
			using (var _dbContext = new GengarContext())
			{
				foreach (var guild in GetGuildInformation())
				{
					var Guild = _discord.GetGuild((ulong)guild.Guildid);
                    Console.WriteLine($"Detected Guild: {Guild.Name}");
					var Channel = Guild.GetTextChannel((ulong)guild.Channelid);
                    Console.WriteLine($"Detected Broadcast Channel: {Channel.Name}");

					var birthday = _dbContext.TblBirthdays.AsNoTracking().Where(d => d.Birthday.Month == DateTime.Now.Month && d.Birthday.Day == DateTime.Now.Day).ToList();

                    Console.WriteLine($"Total birthdays today: {birthday.Count}");

					foreach (var user in birthday.ToList())
					{
						if (Guild.GetUser((ulong)user.Userid) == null)
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


						await Channel.SendMessageAsync(_content);

						foreach (var person in birthday)
						{
							await Channel.SendMessageAsync($"It's <@{person.Userid}> birthday today!! Happy birthday!");
						}
					}
				}
			}
		}
	}
}
