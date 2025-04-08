using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockDamage
{
	public IPlayer ByPlayer;

	public int DecalId;

	public BlockPos Position;

	public BlockFacing Facing;

	public Block Block;

	public float RemainingResistance;

	public long LastBreakEllapsedMs;

	public long BeginBreakEllapsedMs;

	public EnumTool? Tool;

	public int BreakingCounter;
}
