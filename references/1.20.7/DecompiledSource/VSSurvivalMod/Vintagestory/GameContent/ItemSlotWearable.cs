using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSlotWearable : ItemSlot
{
	public string[] canHoldWearableCodes;

	public ItemSlotWearable(InventoryBase inventory, string[] canHoldWearableCodes)
		: base(inventory)
	{
		this.canHoldWearableCodes = canHoldWearableCodes;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		if (!IsDressType(sourceSlot.Itemstack, canHoldWearableCodes))
		{
			return false;
		}
		return base.CanTakeFrom(sourceSlot, priority);
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		if (!IsDressType(itemstackFromSourceSlot.Itemstack, canHoldWearableCodes))
		{
			return false;
		}
		return base.CanHold(itemstackFromSourceSlot);
	}

	public bool IsDressType(ItemStack itemstack, string[] slotWearableCodes)
	{
		if (itemstack == null)
		{
			return false;
		}
		IAttachableToEntity iatta = IAttachableToEntity.FromCollectible(itemstack.Collectible);
		if (iatta != null)
		{
			return slotWearableCodes.IndexOf(iatta.GetCategoryCode(itemstack)) >= 0;
		}
		return false;
	}
}
