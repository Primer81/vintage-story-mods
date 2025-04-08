using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class PlayerPlaceBlockWatcher : PlayerMilestoneWatcherGeneric
{
	public BlockMatcherDelegate BlockMatcher;

	public override void OnBlockPlaced(BlockPos pos, Block block, ItemStack withStackInHands)
	{
		if (BlockMatcher(pos, block, withStackInHands))
		{
			QuantityAchieved++;
			Dirty = true;
		}
	}
}
