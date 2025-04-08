using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class TutorialStepLookatBlock : TutorialStepGeneric
{
	protected PlayerLookatBlockWatcher bwatcher;

	protected override PlayerMilestoneWatcherGeneric watcher => bwatcher;

	public TutorialStepLookatBlock(ICoreClientAPI capi, string text, BlockLookatMatcherDelegate matcher, int goal)
		: base(capi, text)
	{
		bwatcher = new PlayerLookatBlockWatcher();
		bwatcher.BlockMatcher = matcher;
		bwatcher.QuantityGoal = goal;
	}

	public override bool OnBlockPlaced(BlockPos pos, Block block, ItemStack withStackInHands)
	{
		return false;
	}

	public override bool OnBlockLookedAt(BlockSelection currentBlockSelection)
	{
		bwatcher.OnBlockLookedAt(currentBlockSelection);
		if (bwatcher.Dirty)
		{
			bwatcher.Dirty = false;
			return true;
		}
		return false;
	}
}
