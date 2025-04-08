using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorBoatableCrate : CollectibleBehaviorHeldBag, IAttachedInteractions, IAttachedListener
{
	private ICoreAPI Api;

	public CollectibleBehaviorBoatableCrate(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		Api = api;
		base.OnLoaded(api);
	}

	public override bool IsEmpty(ItemStack bagstack)
	{
		return base.IsEmpty(bagstack);
	}

	public override int GetQuantitySlots(ItemStack bagstack)
	{
		if (!(collObj is BlockCrate crate))
		{
			return 0;
		}
		string type = bagstack.Attributes.GetString("type") ?? crate.Props.DefaultType;
		return crate.Props[type].QuantitySlots;
	}

	public override void OnInteract(ItemSlot bagSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
	{
		if ((byEntity.MountedOn?.Controls ?? byEntity.Controls).Sprint)
		{
			return;
		}
		bool put = byEntity.Controls.ShiftKey;
		bool take = !put;
		bool bulk = byEntity.Controls.CtrlKey;
		IPlayer byPlayer = (byEntity as EntityPlayer).Player;
		AttachedContainerWorkspace ws = getOrCreateContainerWorkspace(slotIndex, onEntity, onRequireSave);
		BlockFacing face = BlockFacing.UP;
		Vec3d Pos = byEntity.Pos.XYZ;
		if (!ws.TryLoadInv(bagSlot, slotIndex, onEntity))
		{
			return;
		}
		ItemSlot ownSlot = ws.WrapperInv.FirstNonEmptySlot;
		ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (take && ownSlot != null)
		{
			ItemStack stack = (bulk ? ownSlot.TakeOutWhole() : ownSlot.TakeOut(1));
			int quantity2 = ((!bulk) ? 1 : stack.StackSize);
			if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(stack, Pos.Add(0.5f + face.Normalf.X, 0.5f + face.Normalf.Y, 0.5f + face.Normalf.Z));
			}
			else
			{
				didMoveItems(stack, byPlayer);
			}
			Api.World.Logger.Audit("{0} Took {1}x{2} from Boat crate at {3}.", byPlayer.PlayerName, quantity2, stack.Collectible.Code, Pos);
			ws.BagInventory.SaveSlotIntoBag((ItemSlotBagContent)ownSlot);
		}
		if (!put || hotbarslot.Empty)
		{
			return;
		}
		int quantity = ((!bulk) ? 1 : hotbarslot.StackSize);
		if (ownSlot == null)
		{
			if (hotbarslot.TryPutInto(Api.World, ws.WrapperInv[0], quantity) > 0)
			{
				didMoveItems(ws.WrapperInv[0].Itemstack, byPlayer);
				Api.World.Logger.Audit("{0} Put {1}x{2} into Boat crate at {3}.", byPlayer.PlayerName, quantity, ws.WrapperInv[0].Itemstack.Collectible.Code, Pos);
			}
			ws.BagInventory.SaveSlotIntoBag((ItemSlotBagContent)ws.WrapperInv[0]);
		}
		else if (hotbarslot.Itemstack.Equals(Api.World, ownSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			List<ItemSlot> skipSlots = new List<ItemSlot>();
			while (hotbarslot.StackSize > 0 && skipSlots.Count < ws.WrapperInv.Count)
			{
				WeightedSlot wslot = ws.WrapperInv.GetBestSuitedSlot(hotbarslot, null, skipSlots);
				if (wslot.slot == null)
				{
					break;
				}
				if (hotbarslot.TryPutInto(Api.World, wslot.slot, quantity) > 0)
				{
					didMoveItems(wslot.slot.Itemstack, byPlayer);
					ws.BagInventory.SaveSlotIntoBag((ItemSlotBagContent)wslot.slot);
					Api.World.Logger.Audit("{0} Put {1}x{2} into Boat crate at {3}.", byPlayer.PlayerName, quantity, wslot.slot.Itemstack.Collectible.Code, Pos);
					if (!bulk)
					{
						break;
					}
				}
				skipSlots.Add(wslot.slot);
			}
		}
		hotbarslot.MarkDirty();
	}

	protected void didMoveItems(ItemStack stack, IPlayer byPlayer)
	{
		(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		AssetLocation sound = stack?.Block?.Sounds?.Place;
		Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
	}
}
