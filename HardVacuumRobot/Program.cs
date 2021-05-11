﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OpenHV
{
    public class Program
    {
        public readonly EventId BotEventId = new EventId(42, "HV Bot");

        public DiscordClient Client { get; set; }

        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            var json = "";
            using (var fileStream = File.OpenRead("config.json"))
            using (var streamReader = new StreamReader(fileStream, new UTF8Encoding(false)))
                json = await streamReader.ReadToEndAsync();

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information
            };

            Client = new DiscordClient(config);

            Client.Ready += ClientReady;
            Client.GuildAvailable += GuildAvailable;
            Client.ClientErrored += ClientError;

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        Task ClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");

            return Task.CompletedTask;
        }

        Task GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");

            var channels = e.Guild.Channels;
            var serverWatcher = new ServerWatcher();
            var channel = channels.Where(c => c.Value.Name == "play").Select(c => c.Value).FirstOrDefault();
            if (channel != null)
                serverWatcher.ScanServers(channel).Start();

            return Task.CompletedTask;
        }

        Task ClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

            return Task.CompletedTask;
        }
    }

    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}