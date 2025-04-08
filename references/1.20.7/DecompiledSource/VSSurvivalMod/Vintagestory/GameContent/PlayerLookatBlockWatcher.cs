using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class PlayerLookatBlockWatcher : PlayerMilestoneWatcherGeneric
{
	public BlockLookatMatcherDelegate BlockMatcher;

	public override void OnBlockLookedAt(BlockSelection blockSel)
	{
		if (BlockMatcher(blockSel))
		{
			QuantityAchieved = 1;
			Dirty = true;
		}
	}
}
