using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class EmotionState
{
	public string Code = "";

	public float Duration;

	public float Chance;

	public int Slot;

	public float Priority;

	public float StressLevel;

	public int MaxGeneration = int.MaxValue;

	public EnumAccumType AccumType = EnumAccumType.Max;

	public float whenHealthRelBelow = 999f;

	public bool whenSourceUntargetable;

	public string[] NotifyEntityCodes = new string[0];

	public string[] EntityCodes;

	public AssetLocation[] EntityCodeLocs;

	public float NotifyChances;

	public float NotifyRange = 12f;

	public float BelowTempDuration;

	public float BelowTempThreshold = -9999f;
}
