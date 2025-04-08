using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public class InventoryPlayerMouseCursor : InventoryBasePlayer
{
	private ItemSlot slot;

	private bool wasEmpty = true;

	public override int Count => 1;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId != 0)
			{
				return null;
			}
			return slot;
		}
		set
		{
			if (slotId != 0)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slot = value;
		}
	}

	public InventoryPlayerMouseCursor(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		slot = new ItemSlotUniversal(this);
	}

	public InventoryPlayerMouseCursor(string inventoryId, ICoreAPI api)
		: base(inventoryId, api)
	{
		slot = new ItemSlotUniversal(this);
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		return new WeightedSlot();
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		base.OnItemSlotModified(slot);
		if (wasEmpty && !slot.Empty)
		{
			TreeAttribute tree = new TreeAttribute();
			tree["itemstack"] = new ItemstackAttribute(slot.Itemstack.Clone());
			tree["byentityid"] = new LongAttribute((base.Player?.Entity?.EntityId).GetValueOrDefault());
			Api.Event.PushEvent("onitemgrabbed", tree);
		}
		wasEmpty = slot.Empty;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
	}
}
