using Discord;
using Discord.WebSocket;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HardVacuumRobot
{
	public class Program
	{
		public static readonly IServiceProvider ServiceProvider = CreateProvider();

		static void Main()
		{
			new Program().MainAsync().GetAwaiter().GetResult();
		}

		static IServiceProvider CreateProvider()
		{
			var config = new DiscordSocketConfig()
			{
				GatewayIntents = GatewayIntents.Guilds
					| GatewayIntents.GuildIntegrations
					| GatewayIntents.GuildMessages
			};

			var collection = new ServiceCollection()
				.AddSingleton(config)
				.AddSingleton<DiscordSocketClient>()
				.AddSingleton<ServerWatcher>()
				.AddSingleton<ResourceCenter>();

			return collection.BuildServiceProvider();
		}

		async Task MainAsync()
		{
			var client = ServiceProvider.GetRequiredService<DiscordSocketClient>();

			client.Log += LogAsync;
			client.Ready += ReadyAsync;
			client.MessageReceived += MessageReceivedAsync;

			await client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["DiscordBotToken"]);
			await client.StartAsync();

			await Task.Delay(Timeout.Infinite);
		}

		Task LogAsync(LogMessage log)
		{
			Console.WriteLine(log.Message);
			return Task.CompletedTask;
		}

		Task ReadyAsync()
		{
			var client = ServiceProvider.GetRequiredService<DiscordSocketClient>();
			Console.WriteLine($"{client.CurrentUser} is connected!");

			ServiceProvider.GetRequiredService<ServerWatcher>().Start(client);
			ServiceProvider.GetRequiredService<ResourceCenter>().Observe(client);

			return Task.CompletedTask;
		}

		Task MessageReceivedAsync(SocketMessage message)
		{
			// The bot should never respond to itself.
			var client = ServiceProvider.GetRequiredService<DiscordSocketClient>();
			if (message.Author.Id == client.CurrentUser.Id)
				return Task.CompletedTask;

			ReplayParser.ScanAttachment(message);
			return Task.CompletedTask;
		}
	}
}