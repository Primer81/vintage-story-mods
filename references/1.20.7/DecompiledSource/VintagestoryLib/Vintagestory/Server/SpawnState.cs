using Vintagestory.API.Common.Entities;

namespace Vintagestory.Server;

public class SpawnState
{
	public EntityProperties ForType;

	public EntityProperties[] SelfAndCompanionProps;

	public int SpawnableAmountGlobal;

	public int SpawnCapScaledPerPlayer;

	public int NextGroupSize = -1;

	public string profilerName;

	public int[] surfaceMap;
}
