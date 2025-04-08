using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class TutorialStepPlaceBlock : TutorialStepGeneric
{
	protected PlayerPlaceBlockWatcher bwatcher;

	protected override PlayerMilestoneWatcherGeneric watcher => bwatcher;

	public TutorialStepPlaceBlock(ICoreClientAPI capi, string text, BlockMatcherDelegate matcher, int goal)
		: base(capi, text)
	{
		bwatcher = new PlayerPlaceBlockWatcher();
		bwatcher.BlockMatcher = matcher;
		bwatcher.QuantityGoal = goal;
	}

	public override bool OnBlockPlaced(BlockPos pos, Block block, ItemStack withStackInHands)
	{
		bwatcher.OnBlockPlaced(pos, block, withStackInHands);
		if (bwatcher.Dirty)
		{
			bwatcher.Dirty = false;
			return true;
		}
		return false;
	}
}
