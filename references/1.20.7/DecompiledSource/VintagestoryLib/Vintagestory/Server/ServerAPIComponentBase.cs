namespace Vintagestory.Server;

public abstract class ServerAPIComponentBase
{
	internal ServerMain server;

	public ServerAPIComponentBase(ServerMain server)
	{
		this.server = server;
	}
}
