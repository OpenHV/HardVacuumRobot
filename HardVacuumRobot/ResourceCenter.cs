using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace HardVacuumRobot
{
	public class ResourceCenter
	{
		const string ResourceServerAddress = "https://resource.openra.net/map/hash/";
		const string LastMapAddress = "https://resource.openra.net/map/lastmap/";

		readonly SocketTextChannel channel;
		string lastHash = "";

		public ResourceCenter(DiscordSocketClient client)
		{
			var server = client.GetGuild(ulong.Parse(ConfigurationManager.AppSettings["Server"]));
			channel = server.GetTextChannel(ulong.Parse(ConfigurationManager.AppSettings["DevelopmentChannel"]));
		}

		public async Task RetrieveNewMaps(DiscordSocketClient discordClient)
		{
			Console.WriteLine("Started looking for new maps.");

			while (true)
			{
				if (discordClient.ConnectionState != ConnectionState.Connected)
					continue;

				try
				{
					using var webClient = new WebClient();
					var json = webClient.DownloadString(LastMapAddress);
					var maps = JsonConvert.DeserializeObject<List<Map>>(json);
					foreach (var map in maps)
					{
						if (lastHash == map.Hash)
							continue;

						if (map.Mod != "hv")
							continue;

						var embed = new EmbedBuilder()
							.WithColor(Color.Blue)
							.WithDescription(map.Info)
							.WithTitle(map.Title)
							.WithUrl($"https://resource.openra.net/maps/{map.Id}")
							.WithAuthor("A new map has been uploaded.")
							.WithFooter($"by {map.Author}")
							.WithImageUrl($"https://resource.openra.net/maps/{map.Id}/minimap")
							.WithTimestamp(DateTime.Now);

						await channel.SendMessageAsync(embed: embed.Build());

						lastHash = map.Hash;
					}

					await Task.Delay(TimeSpan.FromHours(1));
				}
				catch (WebException e)
				{
					Console.WriteLine(e.Message);
					await Task.Delay(TimeSpan.FromHours(1));
				}
			}
		}

		public static Map? GetMap(string hash)
		{
			try
			{
				var json = new WebClient().DownloadString($"{ResourceServerAddress}{hash}");
				return JsonConvert.DeserializeObject<List<Map>>(json).First();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			return null;
		}
	}

	public struct Map
	{
		[JsonProperty("id")]
		public int Id { get; private set; }

		[JsonProperty("title")]
		public string Title { get; private set; }

		[JsonProperty("description")]
		public string Description { get; private set; }

		[JsonProperty("info")]
		public string Info { get; private set; }

		[JsonProperty("author")]
		public string Author { get; private set; }

		[JsonProperty("map_type")]
		public string Type { get; private set; }

		[JsonProperty("players")]
		public string Players { get; private set; }

		[JsonProperty("game_mod")]
		public string Mod { get; private set; }

		[JsonProperty("map_hash")]
		public string Hash { get; private set; }

		[JsonProperty("width")]
		public int Width { get; private set; }

		[JsonProperty("height")]
		public int Height { get; private set; }

		[JsonProperty("bounds")]
		public string Bounds { get; private set; }

		[JsonProperty("spawnpoints")]
		public string SpawnPoints { get; private set; }

		[JsonProperty("tileset")]
		public string Tileset { get; private set; }

		[JsonProperty("revision")]
		public int Revision { get; private set; }

		[JsonProperty("last_revision")]
		public bool LastRevision { get; private set; }

		[JsonProperty("requires_upgrade")]
		public bool RequiresUpgrade { get; private set; }

		[JsonProperty("advanced_map")]
		public bool AdvancedMap { get; private set; }

		[JsonProperty("lua")]
		public bool Lua { get; private set; }

		[JsonProperty("posted")]
		public DateTime Posted { get; private set; }

		[JsonProperty("viewed")]
		public int Viewed { get; private set; }

		[JsonProperty("downloaded")]
		public int Downloaded { get; private set; }

		[JsonProperty("rating")]
		public float Rating { get; private set; }

		[JsonProperty("license")]
		public string License { get; private set; }

		[JsonProperty("downloading")]
		public string Downloading { get; private set; }

		[JsonProperty("mapformat")]
		public int MapFormat { get; private set; }

		[JsonProperty("parser")]
		public string Parser { get; private set; }

		[JsonProperty("map_grid_type")]
		public string GridType { get; private set; }

		[JsonProperty("rules")]
		public string Rules { get; private set; }

		[JsonProperty("reports")]
		public string Reports { get; private set; }
	}
}