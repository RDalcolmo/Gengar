using Gengar.Handlers;
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

namespace Gengar.Services
{
	public class GengarService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;

		public GengarService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_discord.MessageReceived += HandleCommandAsync;
			DateTimeHandler.DayChanged += DateTimeHandler_DayChanged;
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

		private async void DateTimeHandler_DayChanged(object sender, DayChangedEventArgs e)
		{
			using (var _dbContext = new GengarContext())
			{
				foreach (var guild in GetGuildInformation())
				{
					var Guild = _discord.GetGuild((ulong)guild.Guildid);
					var Channel = Guild.GetTextChannel((ulong)guild.Channelid);

					var birthday = _dbContext.TblBirthdays.AsQueryable().Where(d => d.Birthday.Value.Month == DateTime.Now.Month && d.Birthday.Value.Day == DateTime.Now.Day).ToList();

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



						await Channel.SendMessageAsync(_content).ConfigureAwait(false);

						foreach (var person in birthday)
						{
							await Channel.SendMessageAsync($"It's <@{person.Userid}> birthday today!! Happy birthday!").ConfigureAwait(false);
						}
					}
				}
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
	}
}
