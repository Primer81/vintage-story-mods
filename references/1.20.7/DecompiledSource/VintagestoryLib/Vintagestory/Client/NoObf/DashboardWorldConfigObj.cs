using System.Collections.Generic;

namespace Vintagestory.Client.NoObf;

public class DashboardWorldConfigObj
{
	public string Seed;

	public string PlayStyle;

	public string PlayStyleLangCode;

	public int MapSizeY = 256;

	public bool RepairMode;

	public Dictionary<string, string> WorldConfiguration;
}
