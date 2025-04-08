using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ItemSlotBuying : ItemSlotSurvival
{
	private InventoryTrader inv;

	public ItemSlotBuying(InventoryTrader inventory)
		: base(inventory)
	{
		inv = inventory;
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		IHeldBag bag = itemstackFromSourceSlot.Itemstack?.Collectible.GetCollectibleInterface<IHeldBag>() ?? null;
		if (base.CanHold(itemstackFromSourceSlot) && (bag == null || bag.IsEmpty(itemstackFromSourceSlot.Itemstack)))
		{
			return IsTraderInterested(itemstackFromSourceSlot);
		}
		return false;
	}

	private bool IsTraderInterested(ItemSlot slot)
	{
		if (slot.Itemstack != null)
		{
			return inv.IsTraderInterestedIn(slot.Itemstack);
		}
		return false;
	}
}
