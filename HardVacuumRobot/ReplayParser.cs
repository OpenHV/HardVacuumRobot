using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
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

		public static void ScanAttachment(SocketMessage message)
		{
			if (message == null || message.Attachments == null)
				return;

			foreach (var attachment in message.Attachments)
			{
				if (!attachment.Filename.EndsWith(".orarep"))
					return;

				var filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
				System.Console.WriteLine($"Parsing attachment {attachment.Filename}");

				try
				{
					using var webClient = new WebClient();
					webClient.DownloadFile(attachment.Url, filePath);

					var miniYaml = ExtractMetaData(filePath).Replace("\t", "  ");
					var yaml = Regex.Replace(miniYaml.Replace("\t", "  "), @"@\d+", "");

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

					var embed = CreateEmbed(metadata, players);
					if (embed != null)
						message.Channel.SendMessageAsync(embed: embed);

					File.Delete(filePath);
				}
				catch (Exception e)
				{
					System.Console.WriteLine(e);
				}
			}
		}

		static string ExtractMetaData(string filePath)
		{
			try
			{
				using (var fileStream = new FileStream(filePath, FileMode.Open))
				{
					if (!fileStream.CanSeek)
						return null;

					if (fileStream.Length < 20)
						return null;

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
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			return null;
		}

		static Embed CreateEmbed(ReplayMetadata metadata, List<Player> players)
		{
			var map = ResourceCenter.GetMap(metadata.Root.MapUid);
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
					IsInline = false,
					Name = "Duration:",
					Value = $"||{metadata.Root.EndTimeUtc - metadata.Root.StartTimeUtc}||"
				}
			};

			var playersByTeam = players.GroupBy(x => x.Team).OrderBy(x => x.Key);
			var teamsCountStr = new List<string>();
			foreach (var kvp in playersByTeam)
			{
				if (kvp.Key == 0)
				{
					teamsCountStr.AddRange(kvp.Select(_ => "1"));

					fields.Add(new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "No team:",
						Value = string.Join("\n", kvp.Select(x => $"{x.Name} [{x.FactionName}]"))
					});
				}
				else
				{
					teamsCountStr.Add(kvp.Count().ToString());
					fields.Add(new EmbedFieldBuilder
					{
						IsInline = true,
						Name = $"Team {kvp.Key}",
						Value = string.Join("\n", kvp.Select(x => $"{x.Name} [{x.FactionName}]"))
					});
				}
			}

			var winners =players.Where(x => x.Outcome == "Won").ToArray();
			string winnerString;
			if (winners.Length == 0)
				winnerString = "Winner is unknown.";
			else if (winners.Length == 1)
				winnerString = $"Winner is {winners[0].Name}";
			else
				winnerString = $"Winners are {string.Join(", ", winners.Select(x => x.Name))}";

			var embed = new EmbedBuilder
			{
				Title = "Replay Preview",
				Description = $"This is a {string.Join("v", teamsCountStr)} game. || {winnerString} ||",
				Footer = new EmbedFooterBuilder
				{
					Text = $"Played {metadata.Root.StartTimeUtc}"
				},
				Color = Color.DarkBlue,
				Fields = fields
			};

			if (map != null)
				embed = embed.WithImageUrl($"https://resource.openra.net/maps/{map.Value.Id}/minimap");

			return embed.Build();
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
