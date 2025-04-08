using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class ServerListEntry : SavegameCellEntry
{
	public string serverName;

	public string serverIp;

	public ServerListPlaystyle playstyle;

	public int maxPlayers;

	public int players;

	public string gameVersion;

	public bool hasPassword;

	public bool whitelisted;

	public string serverUrl;

	public string gameDescription;

	public ModPacket[] mods;
}
