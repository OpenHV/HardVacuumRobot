using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace HardVacuumRobot
{
	class Program
	{
		readonly DiscordSocketClient client;

		CancellationTokenSource cancellationTokenSource;

		static void Main()
		{
			new Program().MainAsync().GetAwaiter().GetResult();
		}

		public Program()
		{
			client = new DiscordSocketClient();
			client.Log += LogAsync;
			client.Ready += ReadyAsync;
			client.MessageReceived += MessageReceivedAsync;
		}

		public async Task MainAsync()
		{
			await client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["DiscordBotToken"]);
			await client.StartAsync();

			await Task.Delay(Timeout.Infinite);
		}

		Task LogAsync(LogMessage log)
		{
			Console.WriteLine(log.ToString());
			return Task.CompletedTask;
		}

		Task ReadyAsync()
		{
			Console.WriteLine($"{client.CurrentUser} is connected!");

			if (cancellationTokenSource != null)
				cancellationTokenSource.Cancel();
			else
				cancellationTokenSource = new CancellationTokenSource();

			var token = cancellationTokenSource.Token;

			var serverWatcher = new ServerWatcher(client);
			Task.Factory.StartNew(() => serverWatcher.ScanServers(client, token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

			var resourceCenter = new ResourceCenter(client);
			Task.Factory.StartNew(() => resourceCenter.RetrieveNewMaps(client, token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

			return Task.CompletedTask;
		}

		Task MessageReceivedAsync(SocketMessage message)
		{
			// The bot should never respond to itself.
			if (message.Author.Id == client.CurrentUser.Id)
				return Task.CompletedTask;

			ReplayParser.ScanAttachment(message);
			return Task.CompletedTask;
		}
	}
}