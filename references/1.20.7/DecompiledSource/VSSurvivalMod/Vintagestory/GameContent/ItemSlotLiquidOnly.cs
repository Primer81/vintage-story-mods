using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ItemSlotLiquidOnly : ItemSlot
{
	public float CapacityLitres;

	public ItemSlotLiquidOnly(InventoryBase inventory, float capacityLitres)
		: base(inventory)
	{
		CapacityLitres = capacityLitres;
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		return BlockLiquidContainerBase.GetContainableProps(itemstackFromSourceSlot.Itemstack) != null;
	}

	public override bool CanTake()
	{
		return true;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		InventoryBase inventoryBase = inventory;
		if (inventoryBase != null && inventoryBase.PutLocked)
		{
			return false;
		}
		ItemStack sourceStack = sourceSlot.Itemstack;
		if (sourceStack == null)
		{
			return false;
		}
		if (BlockLiquidContainerBase.GetContainableProps(sourceStack) != null && (itemstack == null || itemstack.Collectible.GetMergableQuantity(itemstack, sourceStack, priority) > 0))
		{
			return GetRemainingSlotSpace(sourceStack) > 0;
		}
		return false;
	}

	public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (!Empty && sourceSlot.CanHold(this) && (sourceSlot.Itemstack == null || sourceSlot.Itemstack == null || sourceSlot.Itemstack.Collectible.GetMergableQuantity(sourceSlot.Itemstack, itemstack, op.CurrentPriority) >= itemstack.StackSize))
		{
			op.RequestedQuantity = base.StackSize;
			TryPutInto(sourceSlot, ref op);
			if (op.MovedQuantity > 0)
			{
				OnItemSlotModified(itemstack);
			}
		}
	}
}
