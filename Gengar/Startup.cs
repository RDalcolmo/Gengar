using System;
using System.Threading.Tasks;
using Gengar.Services;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gengar
{
	public class Startup
	{
		private readonly DiscordSocketClient _client;

		public static IConfiguration Configuration { get; set; }

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;

			_client = new DiscordSocketClient(new DiscordSocketConfig
			{
				// How much logging do you want to see?
				LogLevel = LogSeverity.Info,
				//MessageCacheSize = 50,
			});

			// Subscribe the logging handler.
			_client.Log += Logger;

			Task.Run(async () =>
			{
				// Login and connect.
				await _client.LoginAsync(TokenType.Bot, Configuration["BotToken"]).ConfigureAwait(false);
				await _client.StartAsync().ConfigureAwait(false);
			});
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(_client);
			services.AddSingleton<CommandService>();
			services.AddSingleton<InteractiveService>();
			services.AddSingleton<GengarService>();
			services.AddHostedService<BroadcastService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			Task.Run(async () =>
			{
				await app.ApplicationServices.GetRequiredService<GengarService>().InitializeAsync(app.ApplicationServices);
			});

			app.UseRouting();
		}

		// Example of a logging handler. This can be re-used by addons
		// that ask for a Func<LogMessage, Task>.
		private static Task Logger(LogMessage message)
		{
			var cc = Console.ForegroundColor;
			switch (message.Severity)
			{
				case LogSeverity.Critical:
				case LogSeverity.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case LogSeverity.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case LogSeverity.Info:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case LogSeverity.Verbose:
				case LogSeverity.Debug:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
			}
			Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");
			Console.ForegroundColor = cc;

			// If you get an error saying 'CompletedTask' doesn't exist,
			// your project is targeting .NET 4.5.2 or lower. You'll need
			// to adjust your project's target framework to 4.6 or higher
			// (instructions for this are easily Googled).
			// If you *need* to run on .NET 4.5 for compat/other reasons,
			// the alternative is to 'return Task.Delay(0);' instead.
			return Task.CompletedTask;
		}
	}
}
