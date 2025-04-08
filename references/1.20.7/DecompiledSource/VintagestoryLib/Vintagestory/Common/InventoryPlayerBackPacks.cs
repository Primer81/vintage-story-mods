using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class InventoryPlayerBackPacks : InventoryBasePlayer
{
	protected ItemSlot[] bagSlots;

	protected BagInventory bagInv;

	public override int CountForNetworkPacket => 4;

	public override int Count => bagSlots.Length + bagInv.Count;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			if (slotId < bagSlots.Length)
			{
				return bagSlots[slotId];
			}
			return bagInv[slotId - bagSlots.Length];
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
			if (slotId < bagSlots.Length)
			{
				bagSlots[slotId] = value;
			}
			bagInv[slotId - bagSlots.Length] = value;
		}
	}

	public InventoryPlayerBackPacks(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		bagSlots = GenEmptySlots(4);
		baseWeight = 1f;
		bagInv = new BagInventory(api, bagSlots);
	}

	public InventoryPlayerBackPacks(string inventoryId, ICoreAPI api)
		: base(inventoryId, api)
	{
		bagSlots = GenEmptySlots(4);
		baseWeight = 1f;
		bagInv = new BagInventory(api, bagSlots);
	}

	public override bool CanPlayerAccess(IPlayer player, EntityPos position)
	{
		return base.CanPlayerAccess(player, position);
	}

	public override void AfterBlocksLoaded(IWorldAccessor world)
	{
		base.AfterBlocksLoaded(world);
		bagInv.ReloadBagInventory(this, bagSlots);
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
		bagSlots = SlotsFromTreeAttributes(tree);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(bagSlots, tree);
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		return new ItemSlotBackpack(this);
	}

	public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		float multiplier = (((sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & EnumItemStorageFlags.Backpack) <= (EnumItemStorageFlags)0) ? 1 : 2);
		if (targetSlot is ItemSlotBagContent && !openedByPlayerGUIds.Contains(playerUID) && !(sourceSlot is DummySlot))
		{
			multiplier *= 0.35f;
		}
		if (targetSlot is ItemSlotBagContent && (targetSlot.StorageType & (targetSlot.StorageType - 1)) == 0 && (targetSlot.StorageType & sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack)) > (EnumItemStorageFlags)0)
		{
			multiplier *= 1.2f;
		}
		float suitability = base.GetSuitability(sourceSlot, targetSlot, isMerge);
		int num;
		if (sourceSlot.Inventory is InventoryGeneric)
		{
			ItemStack itemstack = sourceSlot.Itemstack;
			if (itemstack == null || !itemstack.Collectible.Tool.HasValue)
			{
				num = 1;
				goto IL_00cb;
			}
		}
		num = 0;
		goto IL_00cb;
		IL_00cb:
		return (suitability + (float)num) * multiplier + (float)((sourceSlot is ItemSlotOutput || sourceSlot is ItemSlotCraftingOutput) ? 1 : 0);
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		if (slot is ItemSlotBagContent)
		{
			bagInv.SaveSlotIntoBag((ItemSlotBagContent)slot);
			return;
		}
		bagInv.ReloadBagInventory(this, bagSlots);
		if (Api.Side == EnumAppSide.Server)
		{
			(Api.World.PlayerByUid(playerUID) as IServerPlayer)?.BroadcastPlayerData();
		}
	}

	public override void PerformNotifySlot(int slotId)
	{
		if (this[slotId] is ItemSlotBagContent backpackContent)
		{
			base.PerformNotifySlot(backpackContent.BagIndex);
		}
		base.PerformNotifySlot(slotId);
	}

	public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		bool num = slotId < bagSlots.Length && bagSlots[slotId].Itemstack == null;
		object packet = base.ActivateSlot(slotId, sourceSlot, ref op);
		if (num)
		{
			bagInv.ReloadBagInventory(this, bagSlots);
		}
		return packet;
	}

	public override void DiscardAll()
	{
		for (int i = 0; i < bagSlots.Length; i++)
		{
			if (bagSlots[i].Itemstack != null)
			{
				dirtySlots.Add(i);
			}
			bagSlots[i].Itemstack = null;
		}
		bagInv.ReloadBagInventory(this, bagSlots);
	}

	public override void DropAll(Vec3d pos, int maxStackSize = 0)
	{
		int timer = (base.Player?.Entity?.Properties.Attributes)?["droppedItemsOnDeathTimer"].AsInt(GlobalConstants.TimeToDespawnPlayerInventoryDrops) ?? GlobalConstants.TimeToDespawnPlayerInventoryDrops;
		for (int i = 0; i < bagSlots.Length; i++)
		{
			ItemSlot slot = bagSlots[i];
			if (slot.Itemstack != null)
			{
				EnumHandling handling = EnumHandling.PassThrough;
				slot.Itemstack.Collectible.OnHeldDropped(Api.World, base.Player, slot, slot.StackSize, ref handling);
				if (handling == EnumHandling.PassThrough)
				{
					dirtySlots.Add(i);
					spawnItemEntity(slot.Itemstack, pos, timer);
					slot.Itemstack = null;
				}
			}
		}
		bagInv.ReloadBagInventory(this, bagSlots);
	}
}
