using Gengar.Models;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace Gengar.Services
{
	public class GengarService : IHostedService
	{
		private static Timer _timer;
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;

		public GengarService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_discord.MessageReceived += HandleCommandAsync;
		}

		public async Task InitializeAsync(IServiceProvider provider)
		{
			_provider = provider;
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider).ConfigureAwait(false);
		}

		public List<Tblguilds> GetGuildInformation()
		{
			using (var _dbContext = new GengarContext())
			{
				return _dbContext.TblGuilds.ToList();
			}
		}

		private async Task HandleCommandAsync(SocketMessage arg)
		{
			// Bail out if it's a System Message.
			var msg = arg as SocketUserMessage;
			if (msg == null)
				return;

			// Create a number to track where the prefix ends and the command begins
			int pos = 0;
			// Replace the '!' with whatever character
			// you want to prefix your commands with.
			// Uncomment the second half if you also want
			// commands to be invoked by mentioning the bot instead.
			if (msg.HasCharPrefix('!', ref pos) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */)
			{
				// Create a Command Context.
				var context = new SocketCommandContext(_discord, msg);

				// Execute the command. (result does not indicate a return value, 
				// rather an object stating if the command executed succesfully).
				await _commands.ExecuteAsync(context, pos, _provider).ConfigureAwait(false);

				// Uncomment the following lines if you want the bot
				// to send a message if it failed (not advised for most situations).
				//if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
				//    await msg.Channel.SendMessageAsync(result.ErrorReason);
			}
		}

        public Task StartAsync(CancellationToken cancellationToken)
		{
			TimeSpan interval = TimeSpan.FromHours(24);
			//calculate time to run the first time & delay to set the timer
			//DateTime.Today gives time of midnight 00.00
			var nextRunTime = DateTime.Today.AddDays(1).AddHours(10);
			var curTime = DateTime.Now;
			var firstInterval = nextRunTime.Subtract(curTime);

			Action action = () =>
			{
				var t1 = Task.Delay(firstInterval);
				t1.Wait();
				//Broadcast birthdays
				BroadcastBirthday(null);
				//now schedule it to be called every 24 hours for future
				// timer repeates call to RemoveScheduledAccounts every 24 hours.
				_timer = new Timer(BroadcastBirthday, null, TimeSpan.Zero,	interval);
			};

			// no need to await this call here because this task is scheduled to run much much later.
			Task.Run(action);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

		public void BroadcastBirthday(object state)
        {
			using (var _dbContext = new GengarContext())
			{
				foreach (var guild in GetGuildInformation())
				{
					var Guild = _discord.GetGuild((ulong)guild.Guildid);
					var Channel = Guild.GetTextChannel((ulong)guild.Channelid);

					var birthday = _dbContext.TblBirthdays.AsNoTracking().Where(d => d.Birthday.Month == DateTime.Now.Month && d.Birthday.Day == DateTime.Now.Day).ToList();

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


						Channel.SendMessageAsync(_content).Start();

						foreach (var person in birthday)
						{
							Channel.SendMessageAsync($"It's <@{person.Userid}> birthday today!! Happy birthday!").Start();
						}
					}
				}
			}
		}
    }
}
