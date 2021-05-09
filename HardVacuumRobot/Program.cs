using System.IO;
using System.Text;
using System.Threading.Tasks;
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
                MinimumLogLevel = LogLevel.Debug
            };

            this.Client = new DiscordClient(config);

            this.Client.Ready += this.ClientReady;
            this.Client.GuildAvailable += this.GuildAvailable;
            this.Client.ClientErrored += this.ClientError;

            await this.Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task ClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");

            return Task.CompletedTask;
        }

        private Task GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");

            return Task.CompletedTask;
        }

        private Task ClientError(DiscordClient sender, ClientErrorEventArgs e)
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