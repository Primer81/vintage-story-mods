using System;

namespace Vintagestory.Server.Database;

public class DatabaseUpgrader
{
	private string worldFilename;

	private int curVersion;

	private int destVersion;

	private ServerMain server;

	public DatabaseUpgrader(ServerMain server, string worldFilename, int curVersion, int destVersion)
	{
		this.server = server;
		this.worldFilename = worldFilename;
		this.curVersion = curVersion;
		this.destVersion = destVersion;
	}

	public void PerformUpgrade()
	{
		while (curVersion < destVersion)
		{
			ApplyUpgrader(curVersion + 1);
			curVersion++;
		}
	}

	private void ApplyUpgrader(int curVersion)
	{
		IDatabaseUpgrader upgrader = null;
		if (curVersion == 2)
		{
			upgrader = new DatabaseUpgraderToVersion2();
		}
		if (upgrader == null)
		{
			ServerMain.Logger.Event("No upgrader to " + curVersion + " found.");
			throw new Exception("No upgrader to " + curVersion + " found.");
		}
		upgrader.Upgrade(server, worldFilename);
	}
}
