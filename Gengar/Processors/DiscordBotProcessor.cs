using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Gengar.Options;
using Gengar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;

namespace Gengar.Processors;

public class DiscordBotProcessor : BackgroundService
{
    private static Timer? _timer;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private readonly IServiceProvider _services;
    private readonly BirthdayService _birthdayService;
    private readonly IOptions<DiscordOptions> _options;
    private readonly ILogger<DiscordBotProcessor> _logger;

    public DiscordBotProcessor(DiscordSocketClient client, InteractionService handler,
                               IServiceProvider services, IOptions<DiscordOptions> options,
                               BirthdayService birthdayService, ILogger<DiscordBotProcessor> logger)
    {
        _client = client;
        _handler = handler;
        _services = services;
        _options = options;
        _birthdayService = birthdayService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync();
        await _client.LoginAsync(TokenType.Bot, _options.Value.BotToken);
        await _client.StartAsync();
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

    private async Task DisconnectedAsync(Exception arg)
    {
        _logger.LogInformation("Stopping service: {message}", arg.Message);
        await Task.CompletedTask;
    }

    private async Task ConnectedAsync()
    {
        _logger.LogInformation($"Starting service...");
        await Task.CompletedTask;
    }

    public async void BroadcastBirthday(object? state)
    {
        try
        {
            if (_options.Value.ChannelId == 0)
            {
                return;
            }

            _logger.LogInformation("Broadcasting today's birthdays: {date}",DateTime.Today.ToLongDateString());
            var Guild = _client.Guilds.FirstOrDefault();

            if (Guild == null)
            {
                return;
            }

            _logger.LogInformation("Detected Guild: {guild}", Guild.Name);
            var Channel = Guild.GetTextChannel(_options.Value.ChannelId);

            if (Channel == null)
            {
                return;
            }

            _logger.LogInformation("Detected Broadcast Channel: {channel}", Channel.Name);

            var birthday = await _birthdayService.GetAllUsers();

            birthday = birthday.Where(x => x.Birthday.Month == DateTime.Today.Month && x.Birthday.Day == DateTime.Today.Day && x.CurrentDay != DateTime.Now.DayOfYear).ToList();

            _logger.LogInformation("Total birthdays today: {count}", birthday.Count);

            foreach (var user in birthday.ToList())
            {
                var userInGuild = Guild.GetUser(user._id);

                if (userInGuild == null)
                {
                    birthday.Remove(user);
                }
            }

            var numberOfBirthdays = birthday.Count;

            if (numberOfBirthdays == 0)
            {
                return;
            }

            StringBuilder _content = new($"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} birthdays" : "is 1 birthday")} today!");

            foreach (var person in birthday)
            {
                await _birthdayService.SetCurrentDay(person._id);
                _content.Append($"\nIt's <@{person._id}> birthday today!! Happy birthday!");
            }

            await Channel.SendMessageAsync(_content.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {error}", ex.Message);
        }
    }

    private async Task LogAsync(LogMessage log)
        => await Console.Out.WriteLineAsync(log.ToString());

    private async Task ReadyAsync()
    {
        // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
        // Since Global Commands take around 1 hour to register, we should use a test guild to instantly update and test our commands.

        await _handler.RegisterCommandsGloballyAsync(true);

        TimeSpan interval = TimeSpan.FromMinutes(30);

        void action()
        {
            _logger.LogInformation("Action started.");
            // Schedule it to be called every 24 hours
            // timer repeates call to RemoveScheduledAccounts every 24 hours.
            _timer = new Timer(BroadcastBirthday, null, TimeSpan.Zero, interval);
        }
        _logger.LogInformation("Starting action.");
        // no need to await this call here because this task is scheduled to run much much later.
        await Task.Run(action);
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
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        _logger.LogError("Error: {error}", result.ErrorReason);
                        break;
                    default:
                        break;
                }
            }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
