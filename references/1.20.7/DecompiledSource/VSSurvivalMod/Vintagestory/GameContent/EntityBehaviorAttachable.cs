using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorAttachable : EntityBehaviorContainer, ICustomInteractionHelpPositioning
{
	protected WearableSlotConfig[] wearableSlots;

	protected InventoryGeneric inv;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "wearablesInv";

	public bool TransparentCenter => false;

	public EntityBehaviorAttachable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Api = entity.World.Api;
		wearableSlots = attributes["wearableSlots"].AsObject<WearableSlotConfig[]>();
		inv = new InventoryGeneric(wearableSlots.Length, InventoryClassName + "-" + entity.EntityId, entity.Api, (int id, InventoryGeneric inv) => new ItemSlotWearable(inv, wearableSlots[id].ForCategoryCodes));
		loadInv();
		entity.WatchedAttributes.RegisterModifiedListener("wearablesInv", wearablesModified);
		base.Initialize(properties, attributes);
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		updateSeats();
	}

	private void wearablesModified()
	{
		loadInv();
		updateSeats();
		entity.MarkShapeModified();
	}

	private void updateSeats()
	{
		IVariableSeatsMountable ivsm = entity.GetInterface<IVariableSeatsMountable>();
		if (ivsm == null)
		{
			return;
		}
		for (int i = 0; i < wearableSlots.Length; i++)
		{
			WearableSlotConfig slotcfg = wearableSlots[i];
			slotcfg.SeatConfig = null;
			ItemSlot itemslot = inv[i];
			if (itemslot.Empty)
			{
				if (slotcfg.ProvidesSeatId != null)
				{
					ivsm.RemoveSeat(slotcfg.ProvidesSeatId);
					slotcfg.ProvidesSeatId = null;
				}
				continue;
			}
			SeatConfig attrseatconfig = itemslot.Itemstack?.ItemAttributes?["attachableToEntity"]?["seatConfig"]?.AsObject<SeatConfig>();
			if (attrseatconfig != null)
			{
				attrseatconfig.SeatId = "attachableseat-" + i;
				attrseatconfig.APName = slotcfg.AttachmentPointCode;
				slotcfg.SeatConfig = attrseatconfig;
				ivsm.RegisterSeat(slotcfg.SeatConfig);
				slotcfg.ProvidesSeatId = slotcfg.SeatConfig.SeatId;
			}
			else if (slotcfg.ProvidesSeatId != null)
			{
				ivsm.RemoveSeat(slotcfg.ProvidesSeatId);
			}
		}
	}

	public override bool TryGiveItemStack(ItemStack itemstack, ref EnumHandling handling)
	{
		int index = 0;
		DummySlot sourceslot = new DummySlot(itemstack);
		foreach (ItemSlot item in inv)
		{
			_ = item;
			if (GetSlotFromSelectionBoxIndex(index) != null && TryAttach(sourceslot, index, null))
			{
				handling = EnumHandling.PreventDefault;
				return true;
			}
			index++;
		}
		return base.TryGiveItemStack(itemstack, ref handling);
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		int seleBox = (byEntity as EntityPlayer).EntitySelection?.SelectionBoxIndex ?? (-1);
		if (seleBox <= 0)
		{
			return;
		}
		int index = GetSlotIndexFromSelectionBoxIndex(seleBox - 1);
		ItemSlot slot = ((index >= 0) ? inv[index] : null);
		if (slot == null)
		{
			return;
		}
		handled = EnumHandling.PreventSubsequent;
		EntityControls controls = byEntity.MountedOn?.Controls ?? byEntity.Controls;
		if (mode == EnumInteractMode.Interact && !controls.CtrlKey)
		{
			ItemStack itemstack = slot.Itemstack;
			if (itemstack != null && (itemstack.Collectible.Attributes?.IsTrue("interactPassthrough")).GetValueOrDefault())
			{
				handled = EnumHandling.PassThrough;
				return;
			}
			if (slot.Empty && wearableSlots[index].EmptyInteractPassThrough)
			{
				handled = EnumHandling.PassThrough;
				return;
			}
			if (wearableSlots[index].SeatConfig != null)
			{
				handled = EnumHandling.PassThrough;
				return;
			}
		}
		IAttachedInteractions iai = slot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>();
		if (iai != null)
		{
			EnumHandling itemhanndled = EnumHandling.PassThrough;
			iai.OnInteract(slot, seleBox - 1, entity, byEntity, hitPosition, mode, ref itemhanndled, storeInv);
			if (itemhanndled == EnumHandling.PreventDefault || itemhanndled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (mode != EnumInteractMode.Interact || !controls.CtrlKey)
		{
			handled = EnumHandling.PassThrough;
			return;
		}
		if (!itemslot.Empty)
		{
			if (TryAttach(itemslot, seleBox - 1, byEntity))
			{
				onAttachmentToggled(byEntity, itemslot);
				return;
			}
		}
		else if (TryRemoveAttachment(byEntity, seleBox - 1))
		{
			onAttachmentToggled(byEntity, itemslot);
			return;
		}
		base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
	}

	private void onAttachmentToggled(EntityAgent byEntity, ItemSlot itemslot)
	{
		AssetLocation sound = itemslot.Itemstack?.Block?.Sounds.Place ?? new AssetLocation("sounds/player/build");
		Api.World.PlaySoundAt(sound, entity, (byEntity as EntityPlayer).Player, randomizePitch: true, 16f);
		entity.MarkShapeModified();
		entity.World.BlockAccessor.GetChunkAtBlockPos(entity.ServerPos.AsBlockPos).MarkModified();
	}

	private bool TryRemoveAttachment(EntityAgent byEntity, int slotIndex)
	{
		ItemSlot slot = GetSlotFromSelectionBoxIndex(slotIndex);
		if (slot == null || slot.Empty)
		{
			return false;
		}
		EntityBehaviorSeatable ebh = entity.GetBehavior<EntityBehaviorSeatable>();
		if (ebh != null)
		{
			AttachmentPointAndPose apap = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes[slotIndex];
			string apname = apap.AttachPoint.Code;
			if (ebh.Seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname || seat.Config.SelectionBox == apname)?.Passenger != null)
			{
				(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiredisembark", Lang.Get("Passenger must disembark first before being able to remove this seat"));
				return false;
			}
		}
		IAttachedInteractions obj = slot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>();
		if (obj != null && !obj.OnTryDetach(slot, slotIndex, entity))
		{
			return false;
		}
		EntityBehaviorOwnable ebho = entity.GetBehavior<EntityBehaviorOwnable>();
		if (ebho != null && !ebho.IsOwner(byEntity))
		{
			(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return false;
		}
		bool wasEmptyAlready = slot.StackSize == 0;
		if (wasEmptyAlready || byEntity.TryGiveItemStack(slot.Itemstack))
		{
			(slot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedListener>())?.OnDetached(slot, slotIndex, entity, byEntity);
			if (Api.Side == EnumAppSide.Server && !wasEmptyAlready)
			{
				slot.Itemstack.StackSize = 1;
				Api.World.Logger.Audit("{0} removed from a {1} at {2}, slot {4}: {3}", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos, slot.Itemstack?.ToString(), slotIndex);
			}
			slot.Itemstack = null;
			storeInv();
			return true;
		}
		return false;
	}

	private bool TryAttach(ItemSlot itemslot, int slotIndex, EntityAgent byEntity)
	{
		IAttachableToEntity iatta = IAttachableToEntity.FromCollectible(itemslot.Itemstack.Collectible);
		if (iatta == null || !iatta.IsAttachable(entity, itemslot.Itemstack))
		{
			return false;
		}
		ItemSlot targetSlot = GetSlotFromSelectionBoxIndex(slotIndex);
		string code = iatta.GetCategoryCode(itemslot.Itemstack);
		WearableSlotConfig slotConfig = wearableSlots[slotIndex];
		if (!slotConfig.CanHold(code))
		{
			return false;
		}
		if (!targetSlot.Empty)
		{
			return false;
		}
		EntityBehaviorOwnable ebho = entity.GetBehavior<EntityBehaviorOwnable>();
		if (ebho != null && !ebho.IsOwner(byEntity))
		{
			(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return false;
		}
		if (entity.GetBehavior<EntityBehaviorSeatable>()?.Seats.FirstOrDefault((IMountableSeat s) => s.Config.APName == slotConfig.AttachmentPointCode)?.Passenger != null)
		{
			return false;
		}
		IAttachedInteractions collectibleInterface = itemslot.Itemstack.Collectible.GetCollectibleInterface<IAttachedInteractions>();
		if (collectibleInterface != null && !collectibleInterface.OnTryAttach(itemslot, slotIndex, entity))
		{
			return false;
		}
		IAttachedListener ial = itemslot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedListener>();
		if (entity.World.Side == EnumAppSide.Server)
		{
			string auditLog = string.Format("{0} attached to a {1} at {2}, slot {4}: {3}", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos, itemslot.Itemstack.ToString(), slotIndex);
			bool num = itemslot.TryPutInto(entity.World, targetSlot) > 0;
			if (num)
			{
				Api.World.Logger.Audit(auditLog);
				ial?.OnAttached(itemslot, slotIndex, entity, byEntity);
				storeInv();
			}
			return num;
		}
		return true;
	}

	public ItemSlot GetSlotFromSelectionBoxIndex(int seleBoxIndex)
	{
		int index = GetSlotIndexFromSelectionBoxIndex(seleBoxIndex);
		if (index == -1)
		{
			return null;
		}
		return inv[index];
	}

	public int GetSlotIndexFromSelectionBoxIndex(int seleBoxIndex)
	{
		AttachmentPointAndPose[] seleBoxes = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes;
		if (seleBoxes.Length <= seleBoxIndex || seleBoxIndex < 0)
		{
			return -1;
		}
		string apCode = seleBoxes[seleBoxIndex].AttachPoint.Code;
		return wearableSlots.IndexOf((WearableSlotConfig elem) => elem.AttachmentPointCode == apCode);
	}

	public ItemSlot GetSlotConfigFromAPName(string apCode)
	{
		_ = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes;
		int index = wearableSlots.IndexOf((WearableSlotConfig elem) => elem.AttachmentPointCode == apCode);
		if (index < 0)
		{
			return null;
		}
		return inv[index];
	}

	protected override Shape addGearToShape(Shape entityShape, ItemSlot gearslot, string slotCode, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements, Dictionary<string, StepParentElementTo> overrideStepParent = null)
	{
		int index = gearslot.Inventory.IndexOf((ItemSlot slot) => slot == gearslot);
		overrideStepParent = wearableSlots[index].StepParentTo;
		slotCode = wearableSlots[index].Code;
		return base.addGearToShape(entityShape, gearslot, slotCode, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements, overrideStepParent);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		int i = 0;
		foreach (ItemSlot slot in inv)
		{
			(slot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>())?.OnEntityDespawn(slot, i++, entity, despawn);
		}
		base.OnEntityDespawn(despawn);
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		int i = 0;
		foreach (ItemSlot slot in inv)
		{
			(slot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>())?.OnReceivedClientPacket(slot, i, entity, player, packetid, data, ref handled, storeInv);
			i++;
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		if (es.SelectionBoxIndex > 0)
		{
			return AttachableInteractionHelp.GetOrCreateInteractionHelp(world.Api, this, wearableSlots, es.SelectionBoxIndex - 1, GetSlotFromSelectionBoxIndex(es.SelectionBoxIndex - 1));
		}
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public override string PropertyName()
	{
		return "dressable";
	}

	public void Dispose()
	{
	}

	public Vec3d GetInteractionHelpPosition()
	{
		ICoreClientAPI capi = entity.Api as ICoreClientAPI;
		if (capi.World.Player.CurrentEntitySelection == null)
		{
			return null;
		}
		int selebox = capi.World.Player.CurrentEntitySelection.SelectionBoxIndex - 1;
		if (selebox < 0)
		{
			return null;
		}
		return entity.GetBehavior<EntityBehaviorSelectionBoxes>().GetCenterPosOfBox(selebox)?.Add(0.0, 0.5, 0.0);
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		int i = 0;
		foreach (ItemSlot slot in inv)
		{
			(slot.Itemstack?.Collectible.GetCollectibleInterface<IAttachedInteractions>())?.OnEntityDeath(slot, i++, entity, damageSourceForDeath);
		}
		base.OnEntityDeath(damageSourceForDeath);
	}
}
