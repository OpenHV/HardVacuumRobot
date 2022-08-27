using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using Discord;
using Discord.WebSocket;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;
using System.Linq;

namespace HardVacuumRobot
{
	public class ReplayParser
	{
		const int MetaEndMarker = -2;

		public static async void ScanAttachment(SocketMessage message)
		{
			if (message == null || message.Attachments == null)
				return;

			foreach (var attachment in message.Attachments)
			{
				if (!attachment.Filename.EndsWith(".orarep"))
					return;

				var filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
				Console.WriteLine($"Parsing attachment {attachment.Filename}");
				var yaml = "";

				try
				{
					using (var httpClient = new HttpClient())
					{
						Console.WriteLine($"Downloading to {filePath}");

						using (var stream = await httpClient.GetStreamAsync(attachment.Url))
						{
							using (var fileStream = new FileStream(filePath, FileMode.Create))
							{
								await stream.CopyToAsync(fileStream);
								var miniYaml = ExtractMetaData(fileStream);
								yaml = Regex.Replace(miniYaml.Replace("\t", "  "), @"@\d+", "");
								yaml = yaml.Replace("{DEV_VERSION}", "development");

								var deserializer = new DeserializerBuilder()
									.WithTypeConverter(new DateTimeConverter(DateTimeKind.Utc, CultureInfo.InvariantCulture, new[] { "yyyy-MM-dd HH-mm-ss" }))
									.Build();

								var splitYaml = yaml.Split("Player:");
								var rootYaml = splitYaml[0];
								var metadata = deserializer.Deserialize<ReplayMetadata>(rootYaml);
								var players = new List<Player>();
								foreach (var playerYaml in splitYaml.Skip(1))
								{
									var player = deserializer.Deserialize<Player>(playerYaml);
									players.Add(player);
								}

								var embed = await CreateEmbed(metadata, players);
								if (embed != null)
									await message.Channel.SendMessageAsync(embed: embed);

								File.Delete(filePath);
							}
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Console.WriteLine(yaml);
				}
			}
		}

		static string ExtractMetaData(FileStream fileStream)
		{
			try
			{
				if (!fileStream.CanSeek)
					throw new InvalidOperationException("Can't seek stream.");

				if (fileStream.Length < 20)
					throw new InvalidDataException("File too short.");

				fileStream.Seek(-(4 + 4), SeekOrigin.End);
				var dataLength = fileStream.ReadInt32();
				if (fileStream.ReadInt32() == MetaEndMarker)
				{
					// Go back by (end marker + length storage + data + version + start marker) bytes
					fileStream.Seek(-(4 + 4 + dataLength + 4 + 4), SeekOrigin.Current);

					var unknown1 = fileStream.ReadInt32();
					var unknown2 = fileStream.ReadInt32();
					var unknown3 = fileStream.ReadInt32();

					using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
					var metadata = streamReader.ReadToEnd();
					metadata = metadata.Remove(metadata.Length - 8);
					return metadata;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.StackTrace);
			}

			return null;
		}

		static async Task<Embed> CreateEmbed(ReplayMetadata metadata, List<Player> players)
		{
			var map = await ResourceCenter.GetMap(metadata.Root.MapUid);
			var fields = new List<EmbedFieldBuilder>
			{
				new EmbedFieldBuilder
				{
					IsInline = true,
					Name = "Version:",
					Value = metadata.Root.Version
				},
				new EmbedFieldBuilder
				{
					IsInline = true,
					Name = "Map:",
					Value = map != null ? $"[{metadata.Root.MapTitle}](https://resource.openra.net/maps/{map.Value.Id}/)" : $"{metadata.Root.MapTitle}"
				},
				new EmbedFieldBuilder
				{
					IsInline = true,
					Name = "Duration:",
					Value = $"||{metadata.Root.EndTimeUtc - metadata.Root.StartTimeUtc}||"
				},
				new EmbedFieldBuilder
				{
					IsInline = true,
					Name = "Outcome:",
					Value = $"||{GetWinners(players)}||"
				}
			};

			var playersByTeam = players.GroupBy(x => x.Team).OrderBy(x => x.Key);
			var teamNumbers = new List<string>();
			foreach (var team in playersByTeam)
			{
				if (team.Key == 0)
				{
					teamNumbers.AddRange(team.Select(_ => "1"));

					fields.Add(new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "No team:",
						Value = GetPlayers(team),
					});
				}
				else
				{
					teamNumbers.Add(team.Count().ToString());
					fields.Add(new EmbedFieldBuilder
					{
						IsInline = true,
						Name = $"Team {team.Key}",
						Value = GetPlayers(team),
					});
				}
			}

			var embed = new EmbedBuilder
			{
				Title = "Replay Preview",
				Description = $"This is a {string.Join("v", teamNumbers)} game.",
				Timestamp = metadata.Root.StartTimeUtc,
				Color = Color.DarkBlue,
				Fields = fields
			};

			if (map != null)
				embed = embed.WithImageUrl($"https://resource.openra.net/maps/{map.Value.Id}/minimap");

			return embed.Build();
		}

		static string GetPlayers(IGrouping<int, Player> team)
		{
			return string.Join("\n", team.Select(p =>
				!string.IsNullOrEmpty(p.Fingerprint) ? $"{GetPlayer(p.Fingerprint)} [{p.FactionName}]"
				: $"{p.Name} [{p.FactionName}]"));
		}

		static string GetPlayer(string fingerprint)
		{
			var profile = ForumAuth.GetResponse(fingerprint).Result;
			return $"{profile.Player.ProfileName}";
		}

		static string GetWinners(List<Player> players)
		{
			var winners = players.Where(x => x.Outcome == "Won").ToArray();
			string winnerString;
			if (winners.Length == 0)
				winnerString = "Winner is unknown.";
			else if (winners.Length == 1)
				winnerString = $"Winner is {winners[0].Name}";
			else
				winnerString = $"Winners are {string.Join(", ", winners.Select(x => x.Name))}";

			return winnerString;
		}

		public class ReplayMetadata
		{
			public Root Root { get; set; }
		}

		public class Root
		{
			public string Mod { get; set; }
			public string Version { get; set; }
			public string MapUid { get; set; }
			public string MapTitle { get; set; }
			public int FinalGameTick { get; set; }
			public DateTime StartTimeUtc { get; set; }
			public DateTime EndTimeUtc { get; set; }
			public string DisabledSpawnPoints { get; set; } // TODO: probably wrong
		}

		public class Player
		{
			public int ClientIndex { get; set; }
			public string Name { get; set; }
			public bool IsHuman { get; set; }
			public bool IsBot { get; set; }
			public string FactionName { get; set; }
			public string FactionId { get; set; }
			public string Color { get; set; }
			public string DisplayFactionName { get; set; }
			public string DisplayFactionId { get; set; }
			public int Team { get; set; }
			public int SpawnPoint { get; set; }
			public int Handicap { get; set; }
			public bool IsRandomFaction { get; set; }
			public bool IsRandomSpawnPoint { get; set; }
			public string Fingerprint { get; set; }
			public string Outcome { get; set; }
			public DateTime OutcomeTimestampUtc { get; set; }
			public int DisconnectFrame { get; set; }
		}
	}
}
