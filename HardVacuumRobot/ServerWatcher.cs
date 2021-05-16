using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Threading.Tasks;

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

		public async Task ScanServers(DiscordSocketClient discordClient)
		{
			Console.WriteLine("Started scanning for servers.");

			while (true)
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

						if (server.Players == 0 && WaitingList.Contains(server))
							WaitingList.Remove(server);

						if (server.Players > 0 && !WaitingList.Contains(server))
						{
							var map = ResourceCenter.GetMap(server.Map);

							var embed = new EmbedBuilder()
								.WithColor(Color.Orange)
								.WithDescription($"Join {server.Address}")
								.WithTitle($"{server.Name}")
								.WithAuthor("Server waiting for players.")
								.WithTimestamp(DateTime.Now);

							if (map != null)
								embed = embed
									.WithImageUrl($"https://resource.openra.net/maps/{map.Value.Id}/minimap")
									.WithFooter($"{map.Value.Title} ({map.Value.Players} players)");

							await channel.SendMessageAsync(embed: embed.Build());
							WaitingList.Add(server);
						}
					}

					await Task.Delay(TimeSpan.FromSeconds(5));
				}
				catch (WebException e)
				{
					Console.WriteLine(e.Message);
					await Task.Delay(TimeSpan.FromSeconds(60));
				}
			}
		}
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

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var server = (Server)obj;
			return Address == server.Address;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}