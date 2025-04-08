using Vintagestory.API.Common;

namespace Vintagestory.API.Server;

public class TreeGenParams
{
	public EnumHemisphere hemisphere;

	public bool skipForestFloor;

	public float size = 1f;

	public float vinesGrowthChance;

	public float mossGrowthChance;

	public float otherBlockChance = 1f;

	public int treesInChunkGenerated;
}
