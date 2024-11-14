using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Gengar.Database;
using Gengar.Options;
using Gengar.Processors;
using Gengar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureHostConfiguration(configHost =>
{
    configHost
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddEnvironmentVariables();
});

builder.ConfigureAppConfiguration((hostContext, configBuilder) =>
{
    configBuilder
        .AddEnvironmentVariables()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);

});

builder.ConfigureServices((hostContext, services) =>
{
    services
        .AddOptions()
        .AddSingleton(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.Guilds,
        })
        .AddLogging()
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton<MongoConnector>()
        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
        .AddSingleton<BirthdayService>()
        .Configure<DiscordOptions>(hostContext.Configuration.GetSection(nameof(DiscordOptions)))
        .Configure<MongoDbOptions>(hostContext.Configuration.GetSection(nameof(MongoDbOptions)));

    services.AddHostedService<DiscordBotProcessor>();
});

var app = builder.Build();

app.Run();