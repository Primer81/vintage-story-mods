namespace Vintagestory.Server;

public class RegisterRequestPacket
{
	public ushort port;

	public string name;

	public string icon = "";

	public PlaystylePacket playstyle;

	public ushort maxPlayers;

	public string gameVersion;

	public bool hasPassword;

	public ModPacket[] Mods;

	public string serverUrl;

	public string gameDescription;

	public bool whitelisted;
}
