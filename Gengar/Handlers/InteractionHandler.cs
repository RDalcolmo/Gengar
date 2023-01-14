using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Gengar.Models;
using Gengar.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gengar.Handlers
{
    public class InteractionHandler
    {
        private static Timer? _timer;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly BirthdayService _birthdayService;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, IConfiguration configuration, BirthdayService birthdayService)
        {
            _client = client;
            _handler = handler;
            _services = services;
            _configuration = configuration;
            _birthdayService = birthdayService;
        }

        public async Task InitializeAsync()
        {
            // Process when the client is ready, so we can register our commands.
            _client.Ready += ReadyAsync;
            _client.Connected += ConnectedAsync;
            _client.Disconnected += DisconnectedAsync;
            _handler.Log += LogAsync;

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
        }

        private Task DisconnectedAsync(Exception arg)
        {
            Console.WriteLine($"Stopping service: {arg.Message}");
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        private Task ConnectedAsync()
        {
            TimeSpan interval = TimeSpan.FromHours(24);
            //calculate time to run the first time & delay to set the timer
            //DateTime.Today gives time of midnight 00.00
            var nextRunTime = DateTime.Today.AddDays(1).AddHours(Convert.ToDouble(_configuration["BroadcastTime"]));
            var curTime = DateTime.Now;
            var firstInterval = nextRunTime.Subtract(curTime);

            void action()
            {
                Console.WriteLine("Action started.");
                // Schedule it to be called every 24 hours
                // timer repeates call to RemoveScheduledAccounts every 24 hours.
                _timer = new Timer(BroadcastBirthday, null, TimeSpan.Zero, interval);
            }
            Console.WriteLine("Starting action.");
            // no need to await this call here because this task is scheduled to run much much later.
            Task.Run(action);
            return Task.CompletedTask;
        }

        public async void BroadcastBirthday(object? state)
        {
            Console.WriteLine($"Broadcasting today's birthdays: {DateTime.Today.ToLongDateString()}");
            var Guild = _client.Guilds.FirstOrDefault();

            if (Guild == null)
                return;

            Console.WriteLine($"Detected Guild: {Guild.Name}");
            var Channel = Guild.GetTextChannel(Convert.ToUInt64(_configuration["DiscordChannel"]));

            if (Channel == null)
                return;
            Console.WriteLine($"Detected Broadcast Channel: {Channel.Name}");

            var birthday = await _birthdayService.GetAllUsers();

            birthday = birthday.Where(x => x.Birthday.Month == DateTime.Today.Month && x.Birthday.Day == DateTime.Today.Day).ToList();
            var numberOfBirthdays = birthday.Count;

            if (numberOfBirthdays == 0)
                return;

            Console.WriteLine($"Total birthdays today: {birthday.Count}");

            foreach (var user in birthday.ToList())
            {
                var userInGuild = Guild.GetUser(user._id);

                if (userInGuild == null)
                {
                    birthday.Remove(user);
                }
            }

            string _content = $"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} birthdays" : "is 1 birthday")} today!";

            foreach (var person in birthday)
            {
                _content += $"\nIt's <@{person._id}> birthday today!! Happy birthday!";
            }

            await Channel.SendMessageAsync(_content);
        }

        private async Task LogAsync(LogMessage log)
            => Console.WriteLine(log);

        private async Task ReadyAsync()
        {
            // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
            // Since Global Commands take around 1 hour to register, we should use a test guild to instantly update and test our commands.

            await _handler.RegisterCommandsGloballyAsync(true);
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            Console.WriteLine(result.ErrorReason);
                            break;
                        default:
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
