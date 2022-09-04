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
			client.Ready += RegisterStatusCommand;
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

		async Task RegisterStatusCommand()
		{
			Console.WriteLine("Registering /status command.");
			try
			{
				var client = ServiceProvider.GetRequiredService<DiscordSocketClient>();
				var guild = client.GetGuild(ulong.Parse(ConfigurationManager.AppSettings["Server"]));

				var commands = await guild.GetApplicationCommandsAsync();
				foreach(var command in commands)
 					await command.DeleteAsync();

				var guildCommand = new SlashCommandBuilder();
				guildCommand.WithName("status");
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
				case "status":
					await HandleStatusCommand(command);
					break;
			}
		}

		static async Task HandleStatusCommand(SocketSlashCommand command)
		{
			var serverWatcher = ServiceProvider.GetRequiredService<ServerWatcher>();
			var serverWatcherEmbedBuiler = new EmbedBuilder()
				.WithTitle("Server Watcher")
				.WithDescription($"Last scan for games `{serverWatcher.LastSuccessfulScan().Seconds}` seconds ago.")
				.WithColor(GetStatusColor(serverWatcher.WatchServers.Status))
				.WithCurrentTimestamp();

			var resourceCenter = ServiceProvider.GetRequiredService<ResourceCenter>();
			var resourceCenterEmbedBuiler = new EmbedBuilder()
				.WithTitle("Resource Center")
				.WithDescription($"Last check for maps `{resourceCenter.LastSuccessfulScan().Minutes}` minutes ago.")
				.WithColor(GetStatusColor(resourceCenter.CheckMaps.Status))
				.WithCurrentTimestamp();

			var embeds = new [] { serverWatcherEmbedBuiler.Build(), resourceCenterEmbedBuiler.Build() };
			await command.RespondAsync(embeds: embeds);
		}

		static Color GetStatusColor(TaskStatus status)
		{
			return status switch
			{
				TaskStatus.Created => Color.Blue,
				TaskStatus.Running => Color.Green,
				TaskStatus.RanToCompletion => Color.DarkGreen,
				TaskStatus.WaitingForActivation => Color.LightOrange,
				TaskStatus.WaitingForChildrenToComplete => Color.Orange,
				TaskStatus.WaitingToRun => Color.DarkOrange,
				TaskStatus.Faulted => Color.Red,
				TaskStatus.Canceled => Color.DarkRed,
				_ => Color.Default,
			};
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
