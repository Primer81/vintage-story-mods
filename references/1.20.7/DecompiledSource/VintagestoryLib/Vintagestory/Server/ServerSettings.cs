using Newtonsoft.Json;

namespace Vintagestory.Server;

[JsonObject(MemberSerialization.OptIn)]
public class ServerSettings
{
	public static ServerSettings instance = new ServerSettings();

	[JsonProperty]
	private bool watchModFolder = true;

	[JsonProperty]
	public static string Language = "en";

	public static bool WatchModFolder => instance.watchModFolder;

	public static void Save()
	{
	}

	public static void Load()
	{
		instance = new ServerSettings();
	}
}
