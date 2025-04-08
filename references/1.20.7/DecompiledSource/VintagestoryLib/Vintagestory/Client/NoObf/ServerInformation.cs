using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ServerInformation
{
	internal string ServerName;

	internal ServerConnectData connectdata;

	internal Ping ServerPing;

	internal string Playstyle;

	internal string PlayListCode;

	internal int Seed;

	internal string SavegameIdentifier;

	internal bool RequiresRemappings;

	public ServerInformation()
	{
		ServerName = "";
		connectdata = new ServerConnectData();
		ServerPing = new Ping();
	}
}
