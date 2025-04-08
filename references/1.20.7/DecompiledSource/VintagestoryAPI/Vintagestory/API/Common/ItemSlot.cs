using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// The default item slot to item stacks
/// </summary>
public class ItemSlot
{
	protected ItemStack itemstack;

	protected InventoryBase inventory;

	/// <summary>
	/// Icon name to be drawn in the slot background
	/// </summary>
	public string BackgroundIcon;

	/// <summary>
	/// If set will be used as the background color
	/// </summary>
	public string HexBackgroundColor;

	/// <summary>
	/// The upper holding limit of the slot itself. Standard slots are only limited by the item stacks maxstack size.
	/// </summary>
	public virtual int MaxSlotStackSize { get; set; } = 999999;


	/// <summary>
	/// Gets the inventory attached to this ItemSlot.
	/// </summary>
	public InventoryBase Inventory => inventory;

	public virtual bool DrawUnavailable { get; set; }

	/// <summary>
	/// The ItemStack contained within the slot.
	/// </summary>
	public ItemStack Itemstack
	{
		get
		{
			return itemstack;
		}
		set
		{
			itemstack = value;
		}
	}

	/// <summary>
	/// The number of items in the stack.
	/// </summary>
	public int StackSize
	{
		get
		{
			if (itemstack != null)
			{
				return itemstack.StackSize;
			}
			return 0;
		}
	}

	/// <summary>
	/// Whether or not the stack is empty.
	/// </summary>
	public virtual bool Empty => itemstack == null;

	/// <summary>
	/// The storage type of this slot.
	/// </summary>
	public virtual EnumItemStorageFlags StorageType { get; set; } = EnumItemStorageFlags.General | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Outfit;


	/// <summary>
	/// Can be used to interecept marked dirty calls. 
	/// </summary>
	public event ActionConsumable MarkedDirty;

	/// <summary>
	/// Create a new instance of an item slot
	/// </summary>
	/// <param name="inventory"></param>
	public ItemSlot(InventoryBase inventory)
	{
		this.inventory = inventory;
	}

	/// <summary>
	/// Amount of space left, independent of item MaxStacksize 
	/// </summary>
	public virtual int GetRemainingSlotSpace(ItemStack forItemstack)
	{
		return Math.Max(0, MaxSlotStackSize - StackSize);
	}

	/// <summary>
	/// Whether or not this slot can take the item from the source slot.
	/// </summary>
	/// <param name="sourceSlot"></param>
	/// <param name="priority"></param>
	/// <returns></returns>
	public virtual bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
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
		if ((sourceStack.Collectible.GetStorageFlags(sourceStack) & StorageType) > (EnumItemStorageFlags)0 && (itemstack == null || itemstack.Collectible.GetMergableQuantity(itemstack, sourceStack, priority) > 0))
		{
			return GetRemainingSlotSpace(sourceStack) > 0;
		}
		return false;
	}

	/// <summary>
	/// Whether or not this slot can hold the item from the source slot.
	/// </summary>
	/// <param name="sourceSlot"></param>
	/// <returns></returns>
	public virtual bool CanHold(ItemSlot sourceSlot)
	{
		InventoryBase inventoryBase = inventory;
		if (inventoryBase != null && inventoryBase.PutLocked)
		{
			return false;
		}
		if (sourceSlot?.Itemstack?.Collectible != null && (sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & StorageType) > (EnumItemStorageFlags)0)
		{
			return inventory.CanContain(this, sourceSlot);
		}
		return false;
	}

	/// <summary>
	/// Whether or not this slots item can be retrieved.
	/// </summary>
	/// <returns></returns>
	public virtual bool CanTake()
	{
		InventoryBase inventoryBase = inventory;
		if (inventoryBase != null && inventoryBase.TakeLocked)
		{
			return false;
		}
		return itemstack != null;
	}

	/// <summary>
	/// Gets the entire contents of the stack, setting the base stack to null.
	/// </summary>
	/// <returns></returns>
	public virtual ItemStack TakeOutWhole()
	{
		ItemStack stack = itemstack.Clone();
		itemstack.StackSize = 0;
		itemstack = null;
		OnItemSlotModified(stack);
		return stack;
	}

	/// <summary>
	/// Gets some of the contents of the stack.
	/// </summary>
	/// <param name="quantity">The amount to get from the stack.</param>
	/// <returns>The stack with the quantity take out (or as much as was available)</returns>
	public virtual ItemStack TakeOut(int quantity)
	{
		if (itemstack == null)
		{
			return null;
		}
		if (quantity >= itemstack.StackSize)
		{
			return TakeOutWhole();
		}
		ItemStack emptyClone = itemstack.GetEmptyClone();
		emptyClone.StackSize = quantity;
		itemstack.StackSize -= quantity;
		if (itemstack.StackSize <= 0)
		{
			itemstack = null;
		}
		return emptyClone;
	}

	/// <summary>
	/// Attempts to place item in this slot into the target slot.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="sinkSlot"></param>
	/// <param name="quantity"></param>
	/// <returns>Amount of moved items</returns>
	public virtual int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1)
	{
		ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, quantity);
		return TryPutInto(sinkSlot, ref op);
	}

	/// <summary>
	/// Returns the quantity of items that were not merged (left over in the source slot)
	/// </summary>
	/// <param name="sinkSlot"></param>
	/// <param name="op"></param>
	/// <returns>Amount of moved items</returns>
	public virtual int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!sinkSlot.CanTakeFrom(this) || !CanTake() || itemstack == null)
		{
			return 0;
		}
		InventoryBase inventoryBase = sinkSlot.inventory;
		if (inventoryBase != null && !inventoryBase.CanContain(sinkSlot, this))
		{
			return 0;
		}
		if (sinkSlot.Itemstack == null)
		{
			int q = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
			if (q > 0)
			{
				sinkSlot.Itemstack = TakeOut(q);
				op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, q));
				sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
				OnItemSlotModified(sinkSlot.Itemstack);
			}
			return op.MovedQuantity;
		}
		ItemStackMergeOperation mergeop = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
		int origRequestedQuantity = op.RequestedQuantity;
		op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
		sinkSlot.Itemstack.Collectible.TryMergeStacks(mergeop);
		if (mergeop.MovedQuantity > 0)
		{
			sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			OnItemSlotModified(sinkSlot.Itemstack);
		}
		op.RequestedQuantity = origRequestedQuantity;
		return mergeop.MovedQuantity;
	}

	/// <summary>
	/// Attempts to flip the ItemSlots.
	/// </summary>
	/// <param name="itemSlot"></param>
	/// <returns>Whether or no the flip was successful.</returns>
	public virtual bool TryFlipWith(ItemSlot itemSlot)
	{
		if (itemSlot.StackSize > MaxSlotStackSize)
		{
			return false;
		}
		bool num = (itemSlot.Empty || CanHold(itemSlot)) && (Empty || CanTake());
		bool canHeExchange = (Empty || itemSlot.CanHold(this)) && (itemSlot.Empty || itemSlot.CanTake());
		if (num && canHeExchange)
		{
			itemSlot.FlipWith(this);
			itemSlot.OnItemSlotModified(itemstack);
			OnItemSlotModified(itemSlot.itemstack);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Forces a flip with the given ItemSlot
	/// </summary>
	/// <param name="withSlot"></param>
	protected virtual void FlipWith(ItemSlot withSlot)
	{
		if (withSlot.StackSize > MaxSlotStackSize)
		{
			if (Empty)
			{
				itemstack = withSlot.TakeOut(MaxSlotStackSize);
			}
		}
		else
		{
			ItemStack temp = withSlot.itemstack;
			withSlot.itemstack = itemstack;
			itemstack = temp;
		}
	}

	/// <summary>
	/// Called when a player has clicked on this slot.  The source slot is the mouse cursor slot.  This handles the logic of either taking, putting or exchanging items.
	/// </summary>
	/// <param name="sourceSlot"></param>
	/// <param name="op"></param>
	public virtual void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (Empty && sourceSlot.Empty)
		{
			return;
		}
		switch (op.MouseButton)
		{
		case EnumMouseButton.Left:
			ActivateSlotLeftClick(sourceSlot, ref op);
			break;
		case EnumMouseButton.Middle:
			ActivateSlotMiddleClick(sourceSlot, ref op);
			break;
		case EnumMouseButton.Right:
			ActivateSlotRightClick(sourceSlot, ref op);
			break;
		case EnumMouseButton.Wheel:
			if (op.WheelDir > 0)
			{
				sourceSlot.TryPutInto(this, ref op);
			}
			else
			{
				TryPutInto(sourceSlot, ref op);
			}
			break;
		}
	}

	/// <summary>
	/// Activates the left click functions of the given slot.
	/// </summary>
	/// <param name="sourceSlot"></param>
	/// <param name="op"></param>
	protected virtual void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			if (CanHold(sourceSlot))
			{
				int q = Math.Min(sourceSlot.StackSize, MaxSlotStackSize);
				q = Math.Min(q, GetRemainingSlotSpace(sourceSlot.itemstack));
				itemstack = sourceSlot.TakeOut(q);
				op.MovedQuantity = itemstack.StackSize;
				OnItemSlotModified(itemstack);
			}
			return;
		}
		if (sourceSlot.Empty)
		{
			op.RequestedQuantity = StackSize;
			TryPutInto(sourceSlot, ref op);
			return;
		}
		int maxq = itemstack.Collectible.GetMergableQuantity(itemstack, sourceSlot.itemstack, op.CurrentPriority);
		if (maxq > 0)
		{
			int origRequestedQuantity = op.RequestedQuantity;
			op.RequestedQuantity = GameMath.Min(maxq, sourceSlot.itemstack.StackSize, GetRemainingSlotSpace(sourceSlot.itemstack));
			ItemStackMergeOperation mergeop = (ItemStackMergeOperation)(op = op.ToMergeOperation(this, sourceSlot));
			itemstack.Collectible.TryMergeStacks(mergeop);
			sourceSlot.OnItemSlotModified(itemstack);
			OnItemSlotModified(itemstack);
			op.RequestedQuantity = origRequestedQuantity;
		}
		else
		{
			TryFlipWith(sourceSlot);
		}
	}

	/// <summary>
	/// Activates the middle click functions of the given slot.
	/// </summary>
	/// <param name="sinkSlot"></param>
	/// <param name="op"></param>
	protected virtual void ActivateSlotMiddleClick(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		if (!Empty)
		{
			IPlayer actingPlayer = op.ActingPlayer;
			if (actingPlayer != null && (actingPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative)
			{
				sinkSlot.Itemstack = Itemstack.Clone();
				op.MovedQuantity = Itemstack.StackSize;
				sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
			}
		}
	}

	/// <summary>
	/// Activates the right click functions of the given slot.
	/// </summary>
	/// <param name="sourceSlot"></param>
	/// <param name="op"></param>
	protected virtual void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (Empty)
		{
			if (CanHold(sourceSlot))
			{
				itemstack = sourceSlot.TakeOut(1);
				sourceSlot.OnItemSlotModified(itemstack);
				OnItemSlotModified(itemstack);
			}
		}
		else if (sourceSlot.Empty)
		{
			op.RequestedQuantity = (int)Math.Ceiling((float)itemstack.StackSize / 2f);
			TryPutInto(sourceSlot, ref op);
		}
		else
		{
			op.RequestedQuantity = 1;
			sourceSlot.TryPutInto(this, ref op);
			if (op.MovedQuantity <= 0)
			{
				TryFlipWith(sourceSlot);
			}
		}
	}

	/// <summary>
	/// The event fired when the slot is modified.
	/// </summary>
	/// <param name="sinkStack"></param>
	public virtual void OnItemSlotModified(ItemStack sinkStack)
	{
		if (inventory != null)
		{
			inventory.DidModifyItemSlot(this, sinkStack);
			if (itemstack?.Collectible != null)
			{
				itemstack.Collectible.UpdateAndGetTransitionStates(inventory.Api.World, this);
			}
		}
	}

	/// <summary>
	/// Marks the slot as dirty which  queues it up for saving and resends it to the clients. Does not sync from client to server.
	/// </summary>
	public virtual void MarkDirty()
	{
		if ((this.MarkedDirty == null || !this.MarkedDirty()) && inventory != null)
		{
			inventory.DidModifyItemSlot(this);
			if (itemstack?.Collectible != null)
			{
				itemstack.Collectible.UpdateAndGetTransitionStates(inventory.Api.World, this);
			}
		}
	}

	/// <summary>
	/// Gets the name of the itemstack- if it exists.
	/// </summary>
	/// <returns>The name of the itemStack or null.</returns>
	public virtual string GetStackName()
	{
		return itemstack?.GetName();
	}

	/// <summary>
	/// Gets the StackDescription for the item.
	/// </summary>
	/// <param name="world">The world the item resides in.</param>
	/// <param name="extendedDebugInfo">Whether or not we have Extended Debug Info enabled.</param>
	/// <returns></returns>
	public virtual string GetStackDescription(IClientWorldAccessor world, bool extendedDebugInfo)
	{
		return itemstack?.GetDescription(world, this, extendedDebugInfo);
	}

	public override string ToString()
	{
		if (Empty)
		{
			return base.ToString();
		}
		return base.ToString() + " (" + itemstack.ToString() + ")";
	}

	public virtual WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
	{
		return inventory.GetBestSuitedSlot(sourceSlot, op, skipSlots);
	}
}
