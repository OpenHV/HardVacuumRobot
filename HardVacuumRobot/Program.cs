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

		static void Main(string[] args)
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

			new ServerWatcher(client);

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