using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorSeatable : EntityBehavior, IVariableSeatsMountable, IMountable
{
	public SeatConfig[] SeatConfigs;

	private bool interactMountAnySeat;

	public IMountableSeat[] Seats { get; set; }

	public EntityPos Position => entity.SidedPos;

	private ICoreAPI Api => entity.Api;

	public Entity Controller { get; set; }

	public Entity OnEntity => entity;

	public event CanSitDelegate CanSit;

	public EntityBehaviorSeatable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		SeatConfigs = attributes["seats"].AsObject<SeatConfig[]>();
		interactMountAnySeat = attributes["interactMountAnySeat"].AsBool();
		int i = 0;
		SeatConfig[] seatConfigs = SeatConfigs;
		foreach (SeatConfig seatConfig in seatConfigs)
		{
			if (seatConfig.SeatId == null)
			{
				seatConfig.SeatId = "baseseat-" + i++;
			}
			RegisterSeat(seatConfig);
		}
		base.Initialize(properties, attributes);
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		for (int i = 0; i < Seats.Length; i++)
		{
			IMountableSeat seat = Seats[i];
			if (seat.Config == null)
			{
				Seats = Seats.RemoveAt(i);
				Api.Logger.Warning("Entity {0}, Seat #{1}, id {2} was loaded but not registered, will remove.", entity.Code, i, seat.SeatId);
				i--;
			}
			else if (seat.PassengerEntityIdForInit != 0L && seat.Passenger == null && Api.World.GetEntityById(seat.PassengerEntityIdForInit) is EntityAgent byEntity)
			{
				byEntity.TryMount(seat);
			}
		}
	}

	public bool TryMount(EntityAgent carriedCreature)
	{
		if (carriedCreature != null)
		{
			IMountableSeat[] seats = Seats;
			foreach (IMountableSeat seat in seats)
			{
				if (seat.Passenger == null && carriedCreature.TryMount(seat))
				{
					carriedCreature.Controls.StopAllMovement();
					return true;
				}
			}
		}
		return false;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		if (mode != EnumInteractMode.Interact || !entity.Alive || !byEntity.Alive || !allowSit(byEntity) || itemslot.Itemstack?.Collectible is ItemRope)
		{
			return;
		}
		int seleBox = (byEntity as EntityPlayer).EntitySelection?.SelectionBoxIndex ?? (-1);
		EntityBehaviorSelectionBoxes bhs = entity.GetBehavior<EntityBehaviorSelectionBoxes>();
		if (bhs != null && byEntity.MountedOn == null && seleBox > 0)
		{
			AttachmentPointAndPose apap2 = bhs.selectionBoxes[seleBox - 1];
			string apname2 = apap2.AttachPoint.Code;
			IMountableSeat seat3 = Seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname2 || seat.Config.SelectionBox == apname2);
			if (seat3 != null && seat3.Passenger != null && seat3.Passenger.HasBehavior<EntityBehaviorRopeTieable>())
			{
				(seat3.Passenger as EntityAgent).TryUnmount();
				handled = EnumHandling.PreventSubsequent;
				return;
			}
		}
		if (byEntity.Controls.CtrlKey)
		{
			return;
		}
		if (seleBox > 0 && bhs != null)
		{
			AttachmentPointAndPose apap = bhs.selectionBoxes[seleBox - 1];
			string apname = apap.AttachPoint.Code;
			IMountableSeat seat2 = Seats.FirstOrDefault((IMountableSeat seat) => seat.Config.APName == apname || seat.Config.SelectionBox == apname);
			if (seat2 != null && CanSitOn(seat2) && byEntity.TryMount(seat2))
			{
				handled = EnumHandling.PreventSubsequent;
				if (Api.Side == EnumAppSide.Server)
				{
					Api.World.Logger.Audit("{0} mounts/embarks a {1} at {2}.", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
				return;
			}
			if (!interactMountAnySeat || !itemslot.Empty)
			{
				return;
			}
		}
		mountAnySeat(byEntity, out handled);
	}

	private bool allowSit(EntityAgent byEntity)
	{
		if (this.CanSit == null)
		{
			return true;
		}
		ICoreClientAPI capi = Api as ICoreClientAPI;
		Delegate[] invocationList = this.CanSit.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			if (!((CanSitDelegate)invocationList[i])(byEntity, out var errMsg))
			{
				if (errMsg != null)
				{
					capi?.TriggerIngameError(this, "cantride", Lang.Get("cantride-" + errMsg));
				}
				return false;
			}
		}
		return true;
	}

	private void mountAnySeat(EntityAgent byEntity, out EnumHandling handled)
	{
		handled = EnumHandling.PreventSubsequent;
		IMountableSeat[] seats = Seats;
		foreach (IMountableSeat seat2 in seats)
		{
			if (CanSitOn(seat2) && seat2.CanControl && byEntity.TryMount(seat2))
			{
				if (Api.Side == EnumAppSide.Server)
				{
					Api.World.Logger.Audit("{0} mounts/embarks a {1} at {2}.", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
				return;
			}
		}
		seats = Seats;
		foreach (IMountableSeat seat in seats)
		{
			if (CanSitOn(seat) && byEntity.TryMount(seat))
			{
				if (Api.Side == EnumAppSide.Server)
				{
					Api.World.Logger.Audit("{0} mounts/embarks a {1} at {2}.", byEntity?.GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
				break;
			}
		}
	}

	public bool CanSitOn(IMountableSeat seat)
	{
		if (seat.Passenger != null)
		{
			return false;
		}
		EntityBehaviorAttachable bha = entity.GetBehavior<EntityBehaviorAttachable>();
		if (bha != null)
		{
			ItemSlot slot = bha.GetSlotConfigFromAPName(seat.Config.APName);
			if (slot != null && !slot.Empty)
			{
				JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
				if (itemAttributes == null || !itemAttributes["isSaddle"].AsBool())
				{
					return slot.Itemstack.ItemAttributes?["attachableToEntity"]["seatConfig"].Exists ?? false;
				}
				return true;
			}
		}
		return true;
	}

	public void RegisterSeat(SeatConfig seatconfig)
	{
		if (seatconfig?.SeatId == null)
		{
			throw new ArgumentNullException("seatConfig.SeatId must be set");
		}
		if (Seats == null)
		{
			Seats = new IMountableSeat[0];
		}
		int index = Seats.IndexOf((IMountableSeat s) => s.SeatId == seatconfig.SeatId);
		if (index < 0)
		{
			Seats = Seats.Append(CreateSeat(seatconfig.SeatId, seatconfig));
		}
		else
		{
			Seats[index].Config = seatconfig;
		}
		entity.WatchedAttributes.MarkAllDirty();
	}

	public void RemoveSeat(string seatId)
	{
		int index = Seats.IndexOf((IMountableSeat s) => s.SeatId == seatId);
		if (index >= 0)
		{
			Seats = Seats.RemoveAt(index);
			entity.WatchedAttributes.MarkAllDirty();
		}
	}

	private ITreeAttribute seatsToAttr()
	{
		TreeAttribute tree = new TreeAttribute();
		for (int i = 0; i < Seats.Length; i++)
		{
			IMountableSeat seat = Seats[i];
			tree["s" + i] = new TreeAttribute().Set("passenger", new LongAttribute(seat.Passenger?.EntityId ?? 0)).Set("seatid", new StringAttribute(seat.SeatId));
		}
		return tree;
	}

	private void seatsFromAttr()
	{
		if (entity.WatchedAttributes["seatdata"] is TreeAttribute tree)
		{
			if (Seats == null || Seats.Length != tree.Count)
			{
				Seats = new IMountableSeat[tree.Count];
			}
			for (int i = 0; i < tree.Count; i++)
			{
				TreeAttribute stree = tree["s" + i] as TreeAttribute;
				Seats[i] = CreateSeat((stree["seatid"] as StringAttribute).value, null);
				Seats[i].PassengerEntityIdForInit = (stree["passenger"] as LongAttribute).value;
			}
		}
	}

	protected virtual IMountableSeat CreateSeat(string seatId, SeatConfig config)
	{
		return (entity as ISeatInstSupplier).CreateSeat(this, seatId, config);
	}

	public override void FromBytes(bool isSync)
	{
		seatsFromAttr();
	}

	public override void ToBytes(bool forClient)
	{
		entity.WatchedAttributes["seatdata"] = seatsToAttr();
	}

	public virtual bool AnyMounted()
	{
		return Seats.Any((IMountableSeat seat) => seat.Passenger != null);
	}

	public override string PropertyName()
	{
		return "seatable";
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		if (es.SelectionBoxIndex > 0)
		{
			return SeatableInteractionHelp.GetOrCreateInteractionHelp(world.Api, this, Seats, es.SelectionBoxIndex - 1);
		}
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		base.OnEntityDeath(damageSourceForDeath);
		IMountableSeat[] seats = Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			(seats[i]?.Passenger as EntityAgent)?.TryUnmount();
		}
	}
}
