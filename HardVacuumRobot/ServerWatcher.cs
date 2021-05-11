using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace OpenHV
{
    public class ServerWatcher
    {
        readonly string MasterServerAddress = "https://master.openra.net/games?protocol=2&type=json";
        readonly string ResourceServerAddress = "https://resource.openra.net/map/hash/";
        readonly List<ServerJson> WaitingList = new List<ServerJson>();

        public Task ScanServers(DiscordChannel channel)
        {
            while (true)
            {
                var json = new WebClient().DownloadString(MasterServerAddress);
                var servers = JsonConvert.DeserializeObject<List<ServerJson>>(json);
                foreach (var server in servers)
                {
                    if (server.Mod != "hv")
                        continue;

                    if (server.Players == 0 && WaitingList.Contains(server))
                        WaitingList.Remove(server);

                    if (server.Players > 0 && !WaitingList.Contains(server))
                    {
                        json = new WebClient().DownloadString($"{ResourceServerAddress}{server.Map}");
                        var map = JsonConvert.DeserializeObject<List<MapJson>>(json).First();

                        var embed = new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Orange)
                            .WithDescription($"Join {server.Address}")
                            .WithTitle($"{server.Name}")
                            .WithAuthor("Server waiting for players.")
                            .WithImageUrl($"https://resource.openra.net/maps/{map.Id}/minimap")
                            .WithFooter($"{map.Title} ({map.Players} players)")
                            .WithTimestamp(DateTime.Now);
                        var message = new DiscordMessageBuilder()
                            .WithEmbed(embed);
                        message.SendAsync(channel);

                        WaitingList.Add(server);
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }

    public struct ServerJson
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

            var server = (ServerJson)obj;
            return Address == server.Address;
        }
    }

    public struct MapJson
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