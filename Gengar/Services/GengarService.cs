﻿using Gengar.Models;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
		private readonly IServiceProvider _provider;

		public GengarService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_discord.MessageReceived += HandleCommandAsync;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Console.WriteLine("Starting Gengar service.");
			Task.Run(async () =>
			{
				await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider).ConfigureAwait(false);
			}, cancellationToken);
			
            Console.WriteLine("Command module added successfully.");

			TimeSpan interval = TimeSpan.FromHours(24);
			//calculate time to run the first time & delay to set the timer
			//DateTime.Today gives time of midnight 00.00
			var nextRunTime = DateTime.Today.AddDays(1).AddHours(Convert.ToDouble(Startup.Configuration["BroadcastTime"]));
			var curTime = DateTime.Now;
			var firstInterval = nextRunTime.Subtract(curTime);

            void action()
            {
                Console.WriteLine("Action started.");
                var t1 = Task.Delay(firstInterval, cancellationToken);
                t1.Wait(cancellationToken);
                // Schedule it to be called every 24 hours
                // timer repeates call to RemoveScheduledAccounts every 24 hours.
                _timer = new Timer(BroadcastBirthday, null, TimeSpan.Zero, interval);
            }
            Console.WriteLine("Starting action.");
			// no need to await this call here because this task is scheduled to run much much later.
			Task.Run(action, cancellationToken);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			Console.WriteLine("Disposing timer.");
			_timer?.Dispose();
			return Task.CompletedTask;
		}

		public void BroadcastBirthday(object state)
		{
			Console.WriteLine($"Broadcasting today's birthdays: {DateTime.Today.ToLongDateString()}");
            using var _dbContext = new GengarContext();
            var Guild = _discord.Guilds.FirstOrDefault();

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
                Task.Run(async () =>
                {
                    await Channel.SendMessageAsync(_content).ConfigureAwait(false);
                });

            }
        }

		private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (messageParam is not SocketUserMessage message) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_discord, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(context: context, argPos: argPos, services: _provider);
        }
    }
}
