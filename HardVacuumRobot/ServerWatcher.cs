using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OpenHV
{
    public class ServerWatcher
    {
        readonly string MasterServerAddress = "https://master.openra.net/games?protocol=2&type=json";

        public Task ScanServers(DiscordClient client, DiscordChannel channel)
        {
            while (true)
            {
                var json = new WebClient().DownloadString(MasterServerAddress);
                var servers = JsonConvert.DeserializeObject<List<ServerJson>>(json);
                foreach (var server in servers)
                {
                    if (server.Mod == "hv")
                        client.SendMessageAsync(channel, server.Name);
                }

                Thread.Sleep(5000);
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
    }
}