using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace HardVacuumRobot
{
	public class ForumAuth
	{
		static readonly string InfoAddress = "https://forum.openra.net/openra/info/";
		public static readonly string ProfileAddress = "https://forum.openra.net/memberlist.php?mode=viewprofile&u=";

		public static async Task<Profile> GetResponse(string fingerprint)
		{
			if (string.IsNullOrEmpty(fingerprint))
				return null;

			var miniYaml = "";
			try
			{
				using (var httpClient = new HttpClient())
				{
					var response = await httpClient.GetAsync($"{InfoAddress}{fingerprint}");
					miniYaml = await response.Content.ReadAsStringAsync();
					var yaml = Regex.Replace(miniYaml.Replace("\t", "  "), @"@\d+", "");

					var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
					var splitYaml = yaml.Split("Badges:");
					var rootYaml = splitYaml[0];

					var profile = deserializer.Deserialize<Profile>(yaml);
					return profile;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine(miniYaml);
			}

			return null;
		}

		public class Profile
		{
			public Player Player { get; set; }
		}

		public class Player
		{
			public string Fingerprint { get; set; }
			public string PublicKey { get; set; }
			public bool KeyRevoked { get; set; }
			public int ProfileID { get; set; }
			public string ProfileName { get; set; }
			public string ProfileRank { get; set; }
			public Avatar Avatar { get; set; }
			public Badges Badges { get; set; }
		}

		public class Avatar
		{
			public string Src { get; set; }
			public int Width { get; set; }
			public string Height { get; set; }
		}

		public class Badges
		{
			public Badge Badge { get; set; }
		}

		public class Badge
		{
			public string Label { get; set; }
			public string Icon24 { get; set; }
			public string Icon48 { get; set; }
			public string Icon72 { get; set; }
		}
	}
}
