namespace Vintagestory.Server.Database;

public interface IDatabaseUpgrader
{
	bool Upgrade(ServerMain server, string worldFilename);
}
