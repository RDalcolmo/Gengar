using Birthday_Bot.Handlers;
using Birthday_Bot.Models;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;

namespace Birthday_Bot.Services
{
	public class BirthdayBotService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;
		private IAPIHandler _apiHandler;

		public BirthdayBotService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IAPIHandler apiHandler)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_apiHandler = apiHandler;
			_discord.MessageReceived += HandleCommandAsync;
			_discord.Ready += OnReady;
			DateTimeHandler.DayChanged += DateTimeHandler_DayChanged;
		}

		private async Task OnReady()
		{
			Console.WriteLine("Bot is ready");
			//var user = _discord.GetUser(236922795781521410);
			//await user.SendMessageAsync("Bitch");
		}

		public async Task InitializeAsync(IServiceProvider provider)
		{
			_provider = provider;
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
		}

		public List<Tblguilds> GetGuildInformation()
		{
			using (var db = new BirthdayContext())
			{
				return db.TblGuilds.ToList();
			}
		}

		private async void DateTimeHandler_DayChanged(object sender, DayChangedEventArgs e)
		{
			using (var db = new BirthdayContext())
			{
				foreach (var guild in GetGuildInformation())
				{
					var birthday = db.TblBirthdays.AsQueryable().Where(d => d.Birthday.Value.Month == DateTime.Now.Month && d.Birthday.Value.Day == DateTime.Now.Day).ToList();

					foreach (var user in birthday.ToList())
					{
						if (!await _apiHandler.IsInGuild(guild.Guildid.Value, user.Userid))
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

						MessageModel message = new MessageModel()
						{
							content = _content,
							tts = false
						};

						await _apiHandler.CreateMessage(message, guild.Channelid.Value);

						foreach (var person in birthday)
						{
							MessageModel birthdayMessage = new MessageModel()
							{
								content = $"It's <@{person.Userid}> birthday today!! Happy birthday!",
								tts = false
							};

							await _apiHandler.CreateMessage(birthdayMessage, guild.Channelid.Value);
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
				var result = await _commands.ExecuteAsync(context, pos, _provider);

				// Uncomment the following lines if you want the bot
				// to send a message if it failed (not advised for most situations).
				//if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
				//    await msg.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}
}
