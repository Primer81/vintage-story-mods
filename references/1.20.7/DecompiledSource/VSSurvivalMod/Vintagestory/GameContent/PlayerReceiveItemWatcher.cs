using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class PlayerReceiveItemWatcher : PlayerMilestoneWatcherGeneric
{
	public ItemStackMatcherDelegate StackMatcher;

	public string MatchEventName;

	public override void OnItemStackReceived(ItemStack stack, string eventName)
	{
		if (eventName == MatchEventName && !MilestoneReached() && StackMatcher(stack))
		{
			QuantityAchieved += stack.StackSize;
			Dirty = true;
		}
	}
}
