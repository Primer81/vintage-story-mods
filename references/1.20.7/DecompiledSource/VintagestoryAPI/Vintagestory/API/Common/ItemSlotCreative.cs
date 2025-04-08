using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class ItemSlotCreative : ItemSlot
{
	public ItemSlotCreative(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override ItemStack TakeOutWhole()
	{
		return base.Itemstack.Clone();
	}

	public override ItemStack TakeOut(int quantity)
	{
		ItemStack emptyClone = base.Itemstack.GetEmptyClone();
		emptyClone.StackSize = quantity;
		return emptyClone;
	}

	public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!sinkSlot.CanTakeFrom(this) || !CanTake() || base.Itemstack == null)
		{
			return 0;
		}
		InventoryBase inventoryBase = sinkSlot.Inventory;
		if (inventoryBase != null && !inventoryBase.CanContain(sinkSlot, this))
		{
			return 0;
		}
		if (op.ShiftDown)
		{
			if (Empty)
			{
				return 0;
			}
			int maxstacksize = base.Itemstack.Collectible.MaxStackSize;
			if (sinkSlot.Itemstack == null)
			{
				op.RequestedQuantity = maxstacksize;
			}
			else
			{
				op.RequestedQuantity = maxstacksize - sinkSlot.StackSize;
			}
		}
		if (sinkSlot.Itemstack == null)
		{
			int q = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
			sinkSlot.Itemstack = TakeOut(q);
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			op.MovedQuantity = sinkSlot.StackSize;
			return op.MovedQuantity;
		}
		ItemStack ownStack = base.Itemstack.Clone();
		ItemStackMergeOperation mergeop = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
		int origRequestedQuantity = op.RequestedQuantity;
		op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
		sinkSlot.Itemstack.Collectible.TryMergeStacks(mergeop);
		base.Itemstack = ownStack;
		if (mergeop.MovedQuantity > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
		}
		op.RequestedQuantity = origRequestedQuantity;
		return op.MovedQuantity;
	}

	protected override void ActivateSlotLeftClick(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			sinkSlot.TakeOutWhole();
		}
		else if (sinkSlot.Empty)
		{
			op.RequestedQuantity = base.StackSize;
			TryPutInto(sinkSlot, ref op);
		}
		else if (sinkSlot.Itemstack.Equals(op.World, base.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			op.RequestedQuantity = 1;
			ItemStackMergeOperation mergeop = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
			ItemStack ownStack = base.Itemstack.Clone();
			sinkSlot.Itemstack.Collectible.TryMergeStacks(mergeop);
			base.Itemstack = ownStack;
		}
		else
		{
			sinkSlot.TakeOutWhole();
		}
	}

	protected override void ActivateSlotMiddleClick(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!Empty)
		{
			sinkSlot.Itemstack = base.Itemstack.Clone();
			sinkSlot.Itemstack.StackSize = base.Itemstack.Collectible.MaxStackSize;
			op.MovedQuantity = base.Itemstack.Collectible.MaxStackSize;
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
		}
	}

	protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (!Empty)
		{
			if (sourceSlot.Empty)
			{
				op.RequestedQuantity = 1;
				sourceSlot.TryPutInto(this, ref op);
			}
			else
			{
				sourceSlot.TakeOut(1);
			}
		}
	}

	public override bool TryFlipWith(ItemSlot itemSlot)
	{
		bool num = (itemSlot.Empty || CanHold(itemSlot)) && (Empty || CanTake());
		bool canHeExchange = (Empty || itemSlot.CanHold(this)) && (itemSlot.Empty || itemSlot.CanTake());
		if (num && canHeExchange)
		{
			itemSlot.Itemstack = base.Itemstack.Clone();
			itemSlot.OnItemSlotModified(itemSlot.Itemstack);
			return true;
		}
		return false;
	}

	protected override void FlipWith(ItemSlot withslot)
	{
		ItemStack returnedStack = base.Itemstack.Clone();
		if (withslot.Itemstack != null && withslot.Itemstack.Equals(base.Itemstack))
		{
			returnedStack.StackSize += withslot.Itemstack.StackSize;
		}
		withslot.Itemstack = returnedStack;
	}

	public override void OnItemSlotModified(ItemStack sinkStack)
	{
		if (itemstack != null)
		{
			itemstack.StackSize = 1;
		}
		base.OnItemSlotModified(sinkStack);
	}
}
