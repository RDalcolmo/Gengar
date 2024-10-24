﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Gengar.Database;
using Gengar.Handlers;
using Gengar.Options;
using Gengar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gengar;

public class Program
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _services;

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.Guilds | GatewayIntents.GuildMembers,
        AlwaysDownloadUsers = true,
    };

    public Program()
    {
        _configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", true)
            .Build();

        _services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(_configuration)
            .AddSingleton(_socketConfig)
            .AddSingleton<MongoConnector>()
            .AddSingleton<BirthdayService>()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .Configure<DiscordOptions>(_configuration.GetSection(nameof(DiscordOptions)))
            .BuildServiceProvider();
    }

    static void Main() => new Program().RunAsync().GetAwaiter().GetResult();

    public async Task RunAsync()
    {

        var client = _services.GetRequiredService<DiscordSocketClient>();

        client.Log += LogAsync;

        // Here we can initialize the service that will register and execute our commands
        await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

        // Bot token can be provided from the Configuration object we set up earlier
        await client.LoginAsync(TokenType.Bot, _configuration["BotToken"]);
        await client.StartAsync();

        // Never quit the program until manually forced to.
        await Task.Delay(Timeout.Infinite);
    }

    private async Task LogAsync(LogMessage message) => await Console.Out.WriteLineAsync(message.ToString());
}