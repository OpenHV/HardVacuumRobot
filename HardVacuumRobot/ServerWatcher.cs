using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace HardVacuumRobot
{
	public class ServerWatcher
	{
		readonly string MasterServerAddress = "https://master.openra.net/games?protocol=2&type=json";
		readonly List<Server> WaitingList = new List<Server>();
		readonly SocketTextChannel channel;

		public ServerWatcher(DiscordSocketClient client)
		{
			var server = client.GetGuild(ulong.Parse(ConfigurationManager.AppSettings["Server"]));
			channel = server.GetTextChannel(ulong.Parse(ConfigurationManager.AppSettings["LobbyChannel"]));
		}

		public async Task ScanServers(DiscordSocketClient discordClient, CancellationToken token)
		{
			Console.WriteLine("Started scanning for servers.");

			while (!token.IsCancellationRequested)
			{
				if (discordClient.ConnectionState != ConnectionState.Connected)
					continue;

				try
				{
					using var webClient = new WebClient();
					var json = webClient.DownloadString(MasterServerAddress);
					var servers = JsonConvert.DeserializeObject<List<Server>>(json);
					foreach (var server in servers)
					{
						if (server.Mod != "hv")
							continue;

						if (server.MaxPlayers < 2)
							continue;

						if (server.State != (int)ServerState.WaitingPlayers)
							continue;

						if (server.Players > 0 && !WaitingList.Contains(server))
						{
							var map = ResourceCenter.GetMap(server.Map);

							var color = server.Protected ? Color.Red : Color.Orange;
							var prefix = server.Protected ? "Locked" : "Open";
							var embed = new EmbedBuilder()
								.WithColor(color)
								.WithDescription($"{prefix} server waiting for players.")
								.WithTitle($"{server.Name}")
								.WithAuthor(server.Clients.Single(c => c.IsAdmin).Name)
								.WithTimestamp(DateTime.Now);

							if (map != null)
								embed = embed
									.WithImageUrl($"https://resource.openra.net/maps/{map.Value.Id}/minimap")
									.WithFooter($"{map.Value.Title} ({map.Value.Players} players)");

							await channel.SendMessageAsync(embed: embed.Build());

							System.Console.WriteLine($"Adding {server.Name} ({server.Id}) with {server.Players} players to the waiting list.");
							WaitingList.Add(server);
						}
					}

					foreach(var server in servers)
					{
						if (!WaitingList.Contains(server))
							continue;

						if (server.Players == 0)
						{
							if (WaitingList.Remove(server))
								System.Console.WriteLine($"Removing {server.Name} ({server.Id}) with {server.Players} players from waiting list.");
						}

						if (server.State != (int)ServerState.WaitingPlayers)
						{
							if (WaitingList.Remove(server))
								System.Console.WriteLine($"Removing {server.Name} ({server.Id}) with state {server.State} from waiting list.");
						}
					}

					var removed = WaitingList.RemoveAll(server => !servers.Contains(server));
					if (removed > 0)
						System.Console.WriteLine($"Removing {removed} servers from waiting list as they vanished from the master server.");

					await Task.Delay(TimeSpan.FromSeconds(10));
				}
				catch (WebException e)
				{
					Console.WriteLine(e.Message);
					await Task.Delay(TimeSpan.FromSeconds(60));
				}
			}
		}
	}

	public enum ServerState
	{
		WaitingPlayers = 1,
		GameStarted = 2,
		ShuttingDown = 3
	}

	public struct Server
	{
		[JsonProperty("id")]
		public int Id { get; private set; }

		[JsonProperty("name")]
		public string Name { get; private set; }

		[JsonProperty("address")]
		public string Address { get; private set; }

		[JsonProperty("state")]
		public int State { get; private set; }

		[JsonProperty("mod")]
		public string Mod { get; private set; }

		[JsonProperty("version")]
		public string Version { get; private set; }

		[JsonProperty("modtitle")]
		public string ModTitle { get; private set; }

		[JsonProperty("modwebsite")]
		public string ModWebsite { get; private set; }

		[JsonProperty("modicon32")]
		public string ModIcon32 { get; private set; }

		[JsonProperty("map")]
		public string Map { get; private set; }

		[JsonProperty("players")]
		public int Players { get; private set; }

		[JsonProperty("maxplayers")]
		public int MaxPlayers { get; private set; }

		[JsonProperty("bots")]
		public int Bots { get; private set; }

		[JsonProperty("spectators")]
		public int Spectators { get; private set; }

		[JsonProperty("protected")]
		public bool Protected { get; private set; }

		[JsonProperty("authentication")]
		public bool Authentication { get; private set; }

		[JsonProperty("location")]
		public string Location { get; private set; }

		[JsonProperty("clients")]
		public List<Client> Clients { get; private set; }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var server = (Server)obj;
			return Id == server.Id;
		}

		public override int GetHashCode()
		{
			return Id;
		}
	}

	public struct Client
	{
		[JsonProperty("name")]
		public string Name { get; private set; }

		[JsonProperty("fingerprint")]
		public string Fingerprint { get; private set; }

		[JsonProperty("color")]
		public string Color { get; private set; }

		[JsonProperty("faction")]
		public string Faction { get; private set; }

		[JsonProperty("team")]
		public int Team { get; private set; }

		[JsonProperty("spawnpoint")]
		public int Spawnpoint { get; private set; }

		[JsonProperty("isadmin")]
		public bool IsAdmin { get; private set; }

		[JsonProperty("isspectator")]
		public bool IsSpectator { get; private set; }

		[JsonProperty("isbot")]
		public bool IsBot { get; private set; }
	}
}