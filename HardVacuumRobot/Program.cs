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
			client.Ready += StartServices;
			client.Ready += RegisterDebugCommand;
			client.MessageReceived += MessageReceivedAsync;
			client.SlashCommandExecuted += SlashCommandHandler;

			await client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["DiscordBotToken"]);
			await client.StartAsync();

			await Task.Delay(Timeout.Infinite);
		}

		Task LogAsync(LogMessage log)
		{
			Console.WriteLine(log.Message);
			return Task.CompletedTask;
		}

		Task StartServices()
		{
			var client = ServiceProvider.GetRequiredService<DiscordSocketClient>();
			Console.WriteLine($"{client.CurrentUser} is connected!");

			ServiceProvider.GetRequiredService<ServerWatcher>().Start(client);
			ServiceProvider.GetRequiredService<ResourceCenter>().Observe(client);

			return Task.CompletedTask;
		}

		async Task RegisterDebugCommand()
		{
			System.Console.WriteLine("Registering /debug command.");
			try
			{
				var client = ServiceProvider.GetRequiredService<DiscordSocketClient>();
				var guild = client.GetGuild(ulong.Parse(ConfigurationManager.AppSettings["Server"]));

				var guildCommand = new SlashCommandBuilder();
				guildCommand.WithName("debug");
				guildCommand.WithDescription("Monitor uptime.");
				await guild.CreateApplicationCommandAsync(guildCommand.Build());
			}
			catch(Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

		async Task SlashCommandHandler(SocketSlashCommand command)
		{
			switch(command.Data.Name)
			{
				case "debug":
					await HandleDebugCommand(command);
					break;
			}
		}

		async Task HandleDebugCommand(SocketSlashCommand command)
		{
			var serverWatcher = Program.ServiceProvider.GetRequiredService<ServerWatcher>();
			var serverWatcherEmbedBuiler = new EmbedBuilder()
				.WithTitle("Server Watcher")
				.WithDescription($"Last scan for games `{serverWatcher.LastSuccessfulScan().Seconds}` seconds ago.")
				.WithColor(GetStatusColor(serverWatcher.WatchServers.Status))
				.WithCurrentTimestamp();

			var resourceCenter = Program.ServiceProvider.GetRequiredService<ResourceCenter>();
			var resourceCenterEmbedBuiler = new EmbedBuilder()
				.WithTitle("Resource Center")
				.WithDescription($"Last check for maps `{resourceCenter.LastSuccessfulScan().Minutes}` minutes ago.")
				.WithColor(GetStatusColor(resourceCenter.CheckMaps.Status))
				.WithCurrentTimestamp();

			var embeds = new [] { serverWatcherEmbedBuiler.Build(), resourceCenterEmbedBuiler.Build() };
			await command.RespondAsync(embeds: embeds);
		}

		Color GetStatusColor(TaskStatus status)
		{
			switch (status)
			{
				case TaskStatus.Created:
					return Color.Blue;
				case TaskStatus.Running:
					return Color.Green;
				case TaskStatus.RanToCompletion:
					return Color.DarkGreen;
				case TaskStatus.WaitingForActivation:
					return Color.LightOrange;
				case TaskStatus.WaitingForChildrenToComplete:
					return Color.Orange;
				case TaskStatus.WaitingToRun:
					return Color.DarkOrange;
				case TaskStatus.Faulted:
					return Color.Red;
				case TaskStatus.Canceled:
					return Color.DarkRed;
				default:
					return Color.Default;
			}
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