using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

internal class InventoryPlayerHotbar : InventoryBasePlayer
{
	private ItemSlot[] slots;

	private List<string> mainHandStatMod = new List<string>();

	private List<string> offHandStatMod = new List<string>();

	public override int Count => slots.Length;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			return slots[slotId];
		}
		set
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slots[slotId] = value;
		}
	}

	public InventoryPlayerHotbar(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		slots = GenEmptySlots(12);
		baseWeight = 1.1f;
		InvNetworkUtil = new PlayerInventoryNetworkUtil(this, api);
	}

	public InventoryPlayerHotbar(string inventoryId, ICoreAPI api)
		: base(inventoryId, api)
	{
		slots = GenEmptySlots(12);
		baseWeight = 1.1f;
		InvNetworkUtil = new PlayerInventoryNetworkUtil(this, api);
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		base.OnItemSlotModified(slot);
		updateSlotStatMods(slot);
	}

	public void updateSlotStatMods(ItemSlot slot)
	{
		if (slot is ItemSlotOffhand)
		{
			updateSlotStatMods(offHandStatMod, slot, "offhanditem");
		}
		if (slot == base.Player.InventoryManager.ActiveHotbarSlot)
		{
			DropSlotIfHot(slot, base.Player);
			updateSlotStatMods(mainHandStatMod, slot, "mainhanditem");
		}
	}

	public void updateSlotStatMods(List<string> list, ItemSlot slot, string handcategory)
	{
		IPlayer player = Api.World.PlayerByUid(playerUID);
		if (Api.Side == EnumAppSide.Client)
		{
			player.InventoryManager.BroadcastHotbarSlot();
		}
		if (player.Entity?.Stats == null)
		{
			return;
		}
		foreach (string key2 in list)
		{
			player.Entity.Stats.Remove(key2, handcategory);
		}
		list.Clear();
		if (slot.Empty)
		{
			return;
		}
		JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
		if (itemAttributes == null || !itemAttributes["statModifier"].Exists)
		{
			if (handcategory == "offhanditem")
			{
				player.Entity.Stats.Set("hungerrate", "offhanditem", 0.2f, persistent: true);
				list.Add("hungerrate");
			}
			return;
		}
		JsonObject statmods = slot.Itemstack.ItemAttributes?["statModifier"];
		foreach (JsonObject item in statmods)
		{
			string key = item.AsString();
			player.Entity.Stats.Set(key, handcategory, statmods[key].AsFloat(), persistent: true);
			list.Add(key);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
		slots = SlotsFromTreeAttributes(tree);
		if (slots.Length < 11)
		{
			slots = slots.Append(new ItemSlotSkill(this));
			slots = slots.Append(new ItemSlotOffhand(this));
		}
		else if (slots.Length < 12)
		{
			slots = slots.Append(new ItemSlotOffhand(this));
			if (slots.Length == 12)
			{
				slots[11].Itemstack = slots[10].Itemstack;
				slots[10].Itemstack = null;
			}
		}
	}

	public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		return base.GetSuitability(sourceSlot, targetSlot, isMerge) + ((sourceSlot is ItemSlotGround || sourceSlot is DummySlot) ? 0.5f : 0f);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(slots, tree);
		ResolveBlocksOrItems();
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		return slotId switch
		{
			10 => new ItemSlotSkill(this), 
			11 => new ItemSlotOffhand(this), 
			_ => new ItemSlotSurvival(this)
			{
				BackgroundIcon = ((1 + slotId).ToString() ?? "")
			}, 
		};
	}

	public override void DropAll(Vec3d pos, int maxStackSize = 0)
	{
		slots[10].Itemstack = null;
		base.DropAll(pos, maxStackSize);
	}
}
