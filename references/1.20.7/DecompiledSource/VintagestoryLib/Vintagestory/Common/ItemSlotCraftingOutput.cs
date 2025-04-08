using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public class ItemSlotCraftingOutput : ItemSlotOutput
{
	public bool hasLeftOvers;

	private ItemStack prevStack;

	private InventoryCraftingGrid inv => (InventoryCraftingGrid)inventory;

	public ItemSlotCraftingOutput(InventoryBase inventory)
		: base(inventory)
	{
	}

	protected override void FlipWith(ItemSlot withSlot)
	{
		ItemStackMoveOperation op = new ItemStackMoveOperation(inv.Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, base.StackSize);
		CraftSingle(withSlot, ref op);
	}

	public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			return 0;
		}
		op.RequestedQuantity = base.StackSize;
		ItemStack craftedStack = itemstack.Clone();
		if (hasLeftOvers)
		{
			int moved = base.TryPutInto(sinkSlot, ref op);
			if (!Empty)
			{
				triggerEvent(craftedStack, moved, op.ActingPlayer);
				return moved;
			}
			hasLeftOvers = false;
			inv.ConsumeIngredients(sinkSlot);
			if (inv.CanStillCraftCurrent())
			{
				itemstack = prevStack.Clone();
			}
		}
		if (op.ShiftDown)
		{
			CraftMany(sinkSlot, ref op);
		}
		else
		{
			CraftSingle(sinkSlot, ref op);
		}
		if (op.ActingPlayer != null)
		{
			triggerEvent(craftedStack, op.MovedQuantity, op.ActingPlayer);
		}
		else if (base.Inventory is InventoryBasePlayer playerInventory)
		{
			triggerEvent(craftedStack, op.MovedQuantity, playerInventory.Player);
		}
		return op.MovedQuantity;
	}

	private void triggerEvent(ItemStack craftedStack, int moved, IPlayer actingPlayer)
	{
		TreeAttribute tree = new TreeAttribute();
		craftedStack.StackSize = moved;
		tree["itemstack"] = new ItemstackAttribute(craftedStack);
		tree["byentityid"] = new LongAttribute(actingPlayer.Entity.EntityId);
		actingPlayer.Entity.World.Api.Event.PushEvent("onitemcrafted", tree);
	}

	private void CraftMany(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (itemstack == null)
		{
			return;
		}
		int movedtotal = 0;
		while (true)
		{
			prevStack = itemstack.Clone();
			int stackSize = base.StackSize;
			op.RequestedQuantity = base.StackSize;
			op.MovedQuantity = 0;
			int mv = TryPutIntoNoEvent(sinkSlot, ref op);
			movedtotal += mv;
			if (stackSize > mv)
			{
				hasLeftOvers = mv > 0;
				break;
			}
			inv.ConsumeIngredients(sinkSlot);
			if (!inv.CanStillCraftCurrent())
			{
				break;
			}
			itemstack = prevStack;
		}
		if (movedtotal > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			OnItemSlotModified(sinkSlot.Itemstack);
		}
	}

	public virtual int TryPutIntoNoEvent(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!sinkSlot.CanTakeFrom(this) || !CanTake() || itemstack == null)
		{
			return 0;
		}
		if (sinkSlot.Itemstack == null)
		{
			int q = Math.Min(sinkSlot.GetRemainingSlotSpace(base.Itemstack), op.RequestedQuantity);
			if (q > 0)
			{
				sinkSlot.Itemstack = TakeOut(q);
				op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, q));
			}
			return op.MovedQuantity;
		}
		ItemStackMergeOperation mergeop = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
		int origRequestedQuantity = op.RequestedQuantity;
		op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
		sinkSlot.Itemstack.Collectible.TryMergeStacks(mergeop);
		op.RequestedQuantity = origRequestedQuantity;
		return mergeop.MovedQuantity;
	}

	private void CraftSingle(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		int prevQuantity = base.StackSize;
		int num = TryPutIntoNoEvent(sinkSlot, ref op);
		if (num == prevQuantity)
		{
			inv.ConsumeIngredients(sinkSlot);
		}
		if (num > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			OnItemSlotModified(sinkSlot.Itemstack);
		}
	}
}
