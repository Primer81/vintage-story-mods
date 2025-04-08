using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class ItemOar : Item
{
	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		if ((forEntity as EntityAgent)?.MountedOn != null)
		{
			return null;
		}
		return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
	}
}
