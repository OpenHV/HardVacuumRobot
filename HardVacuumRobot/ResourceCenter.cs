using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace HardVacuumRobot
{
	public class ResourceCenter
	{
		const string ResourceServerAddress = "https://resource.openra.net/map/hash/";

		public ResourceCenter() { }

		public static Map? GetMap(string hash)
		{
			try
			{
				var json = new WebClient().DownloadString($"{ResourceServerAddress}{hash}");
				return JsonConvert.DeserializeObject<List<Map>>(json).First();
			}
			catch (Exception e)
			{
				System.Console.WriteLine(e);
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