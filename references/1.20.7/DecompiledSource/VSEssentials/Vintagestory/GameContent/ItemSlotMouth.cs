using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ItemSlotMouth : ItemSlotSurvival
{
	private EntityBehaviorMouthInventory beh;

	public ItemSlotMouth(EntityBehaviorMouthInventory beh, InventoryGeneric inventory)
		: base(inventory)
	{
		this.beh = beh;
		MaxSlotStackSize = 1;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		if (base.CanTakeFrom(sourceSlot, priority))
		{
			return mouthable(sourceSlot);
		}
		return false;
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		if (base.CanHold(itemstackFromSourceSlot))
		{
			return mouthable(itemstackFromSourceSlot);
		}
		return false;
	}

	public bool mouthable(ItemSlot sourceSlot)
	{
		if (!Empty)
		{
			return false;
		}
		if (beh.PickupCoolDownUntilMs > beh.entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		for (int i = 0; i < beh.acceptStacks.Count; i++)
		{
			if (beh.acceptStacks[i].Equals(beh.entity.World, sourceSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
			{
				return true;
			}
		}
		return beh.entity.World.Rand.NextDouble() < 0.005;
	}
}
