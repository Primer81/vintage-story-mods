using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class MechPowerPath
{
	public BlockFacing OutFacing;

	public bool invert;

	public float gearingRatio;

	private readonly BlockPos fromPos;

	public MechPowerPath()
	{
	}

	public MechPowerPath(BlockFacing facing, float gearingRatioHere, BlockPos fromPos = null, bool inverted = false)
	{
		OutFacing = facing;
		invert = inverted;
		gearingRatio = gearingRatioHere;
		this.fromPos = fromPos;
	}

	public BlockFacing NetworkDir()
	{
		if (!invert)
		{
			return OutFacing;
		}
		return OutFacing.Opposite;
	}

	public bool IsInvertedTowards(BlockPos testPos)
	{
		if (!(fromPos == null))
		{
			return fromPos.AddCopy(NetworkDir()) != testPos;
		}
		return invert;
	}
}
