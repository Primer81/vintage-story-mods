namespace Vintagestory.API.Common;

/// <summary>
/// Standard survival mode slot that can hold everything except full backpacks
/// </summary>
public class ItemSlotSurvival : ItemSlot
{
	public ItemSlotSurvival(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		IHeldBag bag = sourceSlot.Itemstack?.Collectible.GetCollectibleInterface<IHeldBag>() ?? null;
		if (bag != null && !bag.IsEmpty(sourceSlot.Itemstack))
		{
			return false;
		}
		return base.CanTakeFrom(sourceSlot, priority);
	}

	public override bool CanHold(ItemSlot sourceSlot)
	{
		IHeldBag bag = sourceSlot.Itemstack?.Collectible.GetCollectibleInterface<IHeldBag>() ?? null;
		if (base.CanHold(sourceSlot) && (bag == null || bag.IsEmpty(sourceSlot.Itemstack)))
		{
			return inventory.CanContain(this, sourceSlot);
		}
		return false;
	}
}
