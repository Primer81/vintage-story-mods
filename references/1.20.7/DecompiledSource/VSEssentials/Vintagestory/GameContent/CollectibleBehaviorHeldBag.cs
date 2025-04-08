using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorHeldBag : CollectibleBehavior, IHeldBag, IAttachedInteractions, IAttachedListener
{
	public const int PacketIdBitShift = 11;

	private const int defaultFlags = 189;

	public CollectibleBehaviorHeldBag(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
	}

	public void Clear(ItemStack backPackStack)
	{
		backPackStack.Attributes.GetTreeAttribute("backpack")["slots"] = new TreeAttribute();
	}

	public ItemStack[] GetContents(ItemStack bagstack, IWorldAccessor world)
	{
		ITreeAttribute backPackTree = bagstack.Attributes.GetTreeAttribute("backpack");
		if (backPackTree == null)
		{
			return null;
		}
		List<ItemStack> contents = new List<ItemStack>();
		foreach (KeyValuePair<string, IAttribute> item in backPackTree.GetTreeAttribute("slots").SortedCopy())
		{
			ItemStack cstack = (ItemStack)(item.Value?.GetValue());
			cstack?.ResolveBlockOrItem(world);
			contents.Add(cstack);
		}
		return contents.ToArray();
	}

	public virtual bool IsEmpty(ItemStack bagstack)
	{
		ITreeAttribute backPackTree = bagstack.Attributes.GetTreeAttribute("backpack");
		if (backPackTree == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, IAttribute> item in backPackTree.GetTreeAttribute("slots"))
		{
			IItemStack stack = (IItemStack)(item.Value?.GetValue());
			if (stack != null && stack.StackSize > 0)
			{
				return false;
			}
		}
		return true;
	}

	public virtual int GetQuantitySlots(ItemStack bagstack)
	{
		if (bagstack == null || bagstack.Collectible.Attributes == null)
		{
			return 0;
		}
		return bagstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
	}

	public void Store(ItemStack bagstack, ItemSlotBagContent slot)
	{
		bagstack.Attributes.GetTreeAttribute("backpack").GetTreeAttribute("slots")["slot-" + slot.SlotIndex] = new ItemstackAttribute(slot.Itemstack);
	}

	public virtual string GetSlotBgColor(ItemStack bagstack)
	{
		return bagstack.ItemAttributes["backpack"]["slotBgColor"].AsString();
	}

	public virtual EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
	{
		return (EnumItemStorageFlags)bagstack.ItemAttributes["backpack"]["storageFlags"].AsInt(189);
	}

	public List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
	{
		List<ItemSlotBagContent> bagContents = new List<ItemSlotBagContent>();
		string bgcolhex = GetSlotBgColor(bagstack);
		EnumItemStorageFlags flags = GetStorageFlags(bagstack);
		int quantitySlots = GetQuantitySlots(bagstack);
		ITreeAttribute stackBackPackTree = bagstack.Attributes.GetTreeAttribute("backpack");
		if (stackBackPackTree == null)
		{
			stackBackPackTree = new TreeAttribute();
			ITreeAttribute slotsTree = new TreeAttribute();
			for (int slotIndex = 0; slotIndex < quantitySlots; slotIndex++)
			{
				ItemSlotBagContent slot = new ItemSlotBagContent(parentinv, bagIndex, slotIndex, flags);
				slot.HexBackgroundColor = bgcolhex;
				bagContents.Add(slot);
				slotsTree["slot-" + slotIndex] = new ItemstackAttribute(null);
			}
			stackBackPackTree["slots"] = slotsTree;
			bagstack.Attributes["backpack"] = stackBackPackTree;
		}
		else
		{
			foreach (KeyValuePair<string, IAttribute> val in stackBackPackTree.GetTreeAttribute("slots"))
			{
				int slotIndex2 = val.Key.Split("-")[1].ToInt();
				ItemSlotBagContent slot2 = new ItemSlotBagContent(parentinv, bagIndex, slotIndex2, flags);
				slot2.HexBackgroundColor = bgcolhex;
				if (val.Value?.GetValue() != null)
				{
					ItemstackAttribute attr = (ItemstackAttribute)val.Value;
					slot2.Itemstack = attr.value;
					slot2.Itemstack.ResolveBlockOrItem(world);
				}
				while (bagContents.Count <= slotIndex2)
				{
					bagContents.Add(null);
				}
				bagContents[slotIndex2] = slot2;
			}
		}
		return bagContents;
	}

	public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity)
	{
	}

	public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity)
	{
		getOrCreateContainerWorkspace(slotIndex, fromEntity, null).Close((byEntity as EntityPlayer).Player);
	}

	public AttachedContainerWorkspace getOrCreateContainerWorkspace(int slotIndex, Entity onEntity, Action onRequireSave)
	{
		return ObjectCacheUtil.GetOrCreate(onEntity.Api, "att-cont-workspace-" + slotIndex + "-" + onEntity.EntityId + "-" + collObj.Id, () => new AttachedContainerWorkspace(onEntity, onRequireSave));
	}

	public AttachedContainerWorkspace getContainerWorkspace(int slotIndex, Entity onEntity)
	{
		return ObjectCacheUtil.TryGet<AttachedContainerWorkspace>(onEntity.Api, "att-cont-workspace-" + slotIndex + "-" + onEntity.EntityId + "-" + collObj.Id);
	}

	public virtual void OnInteract(ItemSlot bagSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
	{
		if (!(byEntity.MountedOn?.Controls ?? byEntity.Controls).CtrlKey)
		{
			handled = EnumHandling.PreventDefault;
			if (onEntity.Api.Side == EnumAppSide.Client)
			{
				getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave).OnInteract(bagSlot, slotIndex, onEntity, byEntity, hitPosition);
			}
		}
	}

	public void OnReceivedClientPacket(ItemSlot bagSlot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave)
	{
		int targetSlotIndex = packetid >> 11;
		if (slotIndex == targetSlotIndex)
		{
			int first10Bits = 2047;
			packetid &= first10Bits;
			getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave).OnReceivedClientPacket(player, packetid, data, bagSlot, slotIndex, ref handled);
		}
	}

	public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity)
	{
		return true;
	}

	public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity fromEntity)
	{
		return IsEmpty(itemslot.Itemstack);
	}

	public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn)
	{
		if (despawn.Reason == EnumDespawnReason.Death)
		{
			ItemStack[] contents = GetContents(itemslot.Itemstack, onEntity.World);
			foreach (ItemStack stack in contents)
			{
				if (stack != null)
				{
					onEntity.World.SpawnItemEntity(stack, onEntity.Pos.XYZ);
				}
			}
		}
		getContainerWorkspace(slotIndex, onEntity)?.OnDespawn(despawn);
	}

	public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath)
	{
	}
}
