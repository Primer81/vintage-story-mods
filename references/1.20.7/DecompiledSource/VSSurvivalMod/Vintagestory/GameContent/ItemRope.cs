using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ItemRope : Item
{
	private ClothManager cm;

	private SkillItem[] toolModes;

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		int @int = op.SinkSlot.Itemstack.Attributes.GetInt("clothId");
		int srcId = op.SourceSlot.Itemstack.Attributes.GetInt("clothId");
		if (@int != 0 || srcId != 0)
		{
			op.MovableQuantity = 0;
		}
		else
		{
			base.TryMergeStacks(op);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		cm = api.ModLoader.GetModSystem<ClothManager>();
		toolModes = new SkillItem[2]
		{
			new SkillItem
			{
				Code = new AssetLocation("shorten"),
				Name = Lang.Get("Shorten by 1m")
			},
			new SkillItem
			{
				Code = new AssetLocation("length"),
				Name = Lang.Get("Lengthen by 1m")
			}
		};
		if (api is ICoreClientAPI capi)
		{
			toolModes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/shorten.svg"), 48, 48, 5, -1));
			toolModes[0].TexturePremultipliedAlpha = false;
			toolModes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/lengthen.svg"), 48, 48, 5, -1));
			toolModes[1].TexturePremultipliedAlpha = false;
		}
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return toolModes;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return 0;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		int clothId = slot.Itemstack.Attributes.GetInt("clothId");
		ClothSystem sys = null;
		if (clothId != 0)
		{
			sys = cm.GetClothSystem(clothId);
		}
		if (sys == null)
		{
			return;
		}
		if (toolMode == 0)
		{
			if (!sys.ChangeRopeLength(-0.5))
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "tooshort", Lang.Get("Already at minimum length!"));
			}
			else if (api is ICoreServerAPI sapi2)
			{
				sapi2.Network.GetChannel("clothphysics").BroadcastPacket(new ClothLengthPacket
				{
					ClothId = sys.ClothId,
					LengthChange = -0.5
				}, byPlayer as IServerPlayer);
			}
		}
		if (toolMode == 1)
		{
			if (!sys.ChangeRopeLength(0.5))
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "tooshort", Lang.Get("Already at maximum length!"));
			}
			else if (api is ICoreServerAPI sapi)
			{
				sapi.Network.GetChannel("clothphysics").BroadcastPacket(new ClothLengthPacket
				{
					ClothId = sys.ClothId,
					LengthChange = 0.5
				}, byPlayer as IServerPlayer);
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		handling = EnumHandHandling.PreventDefault;
		int clothId = slot.Itemstack.Attributes.GetInt("clothId");
		ClothSystem sys = null;
		if (clothId != 0)
		{
			sys = cm.GetClothSystem(clothId);
			if (sys == null)
			{
				clothId = 0;
			}
		}
		ClothPoint[] pEnds = sys?.Ends;
		if (sys == null)
		{
			if (blockSel != null && api.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorRopeTieable>())
			{
				sys = attachToBlock(byEntity, blockSel.Position, slot, null);
			}
			else if (entitySel != null)
			{
				sys = attachToEntity(byEntity, entitySel, slot, null, out var relayRopeInteractions2);
				if (relayRopeInteractions2)
				{
					handling = EnumHandHandling.NotHandled;
					if (sys != null)
					{
						splitStack(slot, byEntity);
					}
					return;
				}
			}
			if (sys != null)
			{
				splitStack(slot, byEntity);
			}
		}
		else
		{
			if (blockSel != null && (blockSel.Position.Equals(pEnds[0].PinnedToBlockPos) || blockSel.Position.Equals(pEnds[1].PinnedToBlockPos)))
			{
				detach(sys, slot, byEntity, null, blockSel.Position);
				return;
			}
			if (entitySel != null && (entitySel.Entity.EntityId == pEnds[0].PinnedToEntity?.EntityId || entitySel.Entity.EntityId == pEnds[1].PinnedToEntity?.EntityId))
			{
				detach(sys, slot, byEntity, entitySel.Entity, null);
				return;
			}
			if (blockSel != null && api.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorRopeTieable>())
			{
				sys = attachToBlock(byEntity, blockSel.Position, slot, sys);
				pEnds = sys?.Ends;
			}
			else if (entitySel != null)
			{
				attachToEntity(byEntity, entitySel, slot, sys, out var relayRopeInteractions);
				if (relayRopeInteractions)
				{
					handling = EnumHandHandling.NotHandled;
					return;
				}
			}
		}
		if (clothId == 0 && sys != null)
		{
			sys.WalkPoints(delegate(ClothPoint p)
			{
				p.update(0f, api.World);
			});
			sys.setRenderCenterPos();
		}
		if (pEnds != null && pEnds[0].PinnedToEntity?.EntityId != byEntity.EntityId && pEnds[1].PinnedToEntity?.EntityId != byEntity.EntityId)
		{
			slot.Itemstack.Attributes.RemoveAttribute("clothId");
			slot.TakeOut(1);
			slot.MarkDirty();
		}
	}

	private void splitStack(ItemSlot slot, EntityAgent byEntity)
	{
		if (slot.StackSize > 1)
		{
			ItemStack split = slot.TakeOut(slot.StackSize - 1);
			split.Attributes.RemoveAttribute("clothId");
			split.Attributes.RemoveAttribute("ropeHeldByEntityId");
			if (!byEntity.TryGiveItemStack(split))
			{
				api.World.SpawnItemEntity(split, byEntity.ServerPos.XYZ);
			}
		}
	}

	private ClothSystem createRope(ItemSlot slot, EntityAgent byEntity, Vec3d targetPos)
	{
		ClothSystem sys = ClothSystem.CreateRope(api, cm, byEntity.Pos.XYZ, targetPos, null);
		Vec3d aheadPos = new Vec3d(0.0, byEntity.LocalEyePos.Y - 0.30000001192092896, 0.0).AheadCopy(0.10000000149011612, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw).AheadCopy(0.4000000059604645, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw - (float)Math.PI / 2f);
		_ = byEntity.SidedPos;
		sys.FirstPoint.PinTo(byEntity, aheadPos.ToVec3f());
		cm.RegisterCloth(sys);
		slot.Itemstack.Attributes.SetLong("ropeHeldByEntityId", byEntity.EntityId);
		slot.Itemstack.Attributes.SetInt("clothId", sys.ClothId);
		slot.MarkDirty();
		return sys;
	}

	private void detach(ClothSystem sys, ItemSlot slot, EntityAgent byEntity, Entity toEntity, BlockPos pos)
	{
		toEntity?.GetBehavior<EntityBehaviorRopeTieable>()?.Detach(sys);
		sys.WalkPoints(delegate(ClothPoint point)
		{
			if (point.PinnedToBlockPos != null && point.PinnedToBlockPos.Equals(pos))
			{
				point.UnPin();
			}
			if (point.PinnedToEntity?.EntityId == byEntity.EntityId)
			{
				point.UnPin();
			}
		});
		if (!sys.PinnedAnywhere)
		{
			slot.Itemstack.Attributes.RemoveAttribute("clothId");
			slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
			cm.UnregisterCloth(sys.ClothId);
		}
	}

	private ClothSystem attachToEntity(EntityAgent byEntity, EntitySelection toEntitySel, ItemSlot slot, ClothSystem sys, out bool relayRopeInteractions)
	{
		relayRopeInteractions = false;
		Entity toEntity = toEntitySel.Entity;
		EntityBehaviorOwnable ebho = toEntity.GetBehavior<EntityBehaviorOwnable>();
		if (ebho != null && !ebho.IsOwner(byEntity))
		{
			(toEntity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return null;
		}
		IRopeTiedCreatureCarrier icc = toEntity.GetInterface<IRopeTiedCreatureCarrier>();
		if (sys != null && icc != null)
		{
			ClothPoint[] pEnds = sys.Ends;
			ClothPoint elkPoint = ((pEnds[0].PinnedToEntity?.EntityId == byEntity.EntityId && pEnds[1].Pinned) ? pEnds[1] : pEnds[0]);
			if (icc.TryMount(elkPoint.PinnedToEntity as EntityAgent))
			{
				cm.UnregisterCloth(sys.ClothId);
				return null;
			}
		}
		if (!toEntity.HasBehavior<EntityBehaviorRopeTieable>())
		{
			relayRopeInteractions = (toEntity?.Properties.Attributes?["relayRopeInteractions"].AsBool(defaultValue: true)).GetValueOrDefault();
			if (!relayRopeInteractions && api.World.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "notattachable", Lang.Get("This creature is not tieable"));
			}
			return null;
		}
		EntityBehaviorRopeTieable bh = toEntity.GetBehavior<EntityBehaviorRopeTieable>();
		if (!bh.CanAttach())
		{
			return null;
		}
		if (sys == null)
		{
			sys = createRope(slot, byEntity, toEntity.SidedPos.XYZ);
			bh.Attach(sys, sys.LastPoint);
		}
		else
		{
			ClothPoint[] pEnds2 = sys.Ends;
			ClothPoint cpoint = ((pEnds2[0].PinnedToEntity?.EntityId == byEntity.EntityId && pEnds2[1].Pinned) ? pEnds2[0] : pEnds2[1]);
			bh.Attach(sys, cpoint);
		}
		return sys;
	}

	private ClothSystem attachToBlock(EntityAgent byEntity, BlockPos toPosition, ItemSlot slot, ClothSystem sys)
	{
		if (sys == null)
		{
			sys = createRope(slot, byEntity, toPosition.ToVec3d().Add(0.5, 0.5, 0.5));
			sys.LastPoint.PinTo(toPosition, new Vec3f(0.5f, 0.5f, 0.5f));
		}
		else
		{
			ClothPoint[] pEnds = sys.Ends;
			ClothPoint cpoint = pEnds[0];
			Entity startEntity = pEnds[0].PinnedToEntity;
			Entity endEntity = pEnds[1].PinnedToEntity;
			Entity fromEntity = startEntity ?? endEntity;
			if (startEntity?.EntityId != byEntity.EntityId)
			{
				cpoint = pEnds[1];
			}
			if (fromEntity == byEntity)
			{
				fromEntity = endEntity ?? startEntity;
			}
			if (fromEntity is EntityAgent agent && ((startEntity != null && startEntity != byEntity) || (endEntity != null && endEntity != byEntity)))
			{
				cm.UnregisterCloth(sys.ClothId);
				sys = createRope(slot, agent, toPosition.ToVec3d().Add(0.5, 0.5, 0.5));
				sys.LastPoint.PinTo(toPosition, new Vec3f(0.5f, 0.5f, 0.5f));
			}
			else
			{
				cpoint.PinTo(toPosition, new Vec3f(0.5f, 0.5f, 0.5f));
			}
		}
		return sys;
	}

	public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
	{
		if (!(slot.Inventory is InventoryBasePlayer))
		{
			if (slot.Itemstack.Attributes.GetLong("ropeHeldByEntityId", 0L) != 0L)
			{
				slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
			}
			int clothId = slot.Itemstack.Attributes.GetInt("clothId");
			if (clothId != 0)
			{
				cm.GetClothSystem(clothId);
			}
		}
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		long ropeHeldByEntityId = entityItem.Itemstack.Attributes.GetLong("ropeHeldByEntityId", 0L);
		if (ropeHeldByEntityId == 0L)
		{
			return;
		}
		entityItem.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
		int clothId = entityItem.Itemstack.Attributes.GetInt("clothId");
		if (clothId == 0)
		{
			return;
		}
		ClothSystem sys = cm.GetClothSystem(clothId);
		if (sys != null)
		{
			ClothPoint p = null;
			Entity pinnedToEntity = sys.FirstPoint.PinnedToEntity;
			if (pinnedToEntity != null && pinnedToEntity.EntityId == ropeHeldByEntityId)
			{
				p = sys.FirstPoint;
			}
			Entity pinnedToEntity2 = sys.LastPoint.PinnedToEntity;
			if (pinnedToEntity2 != null && pinnedToEntity2.EntityId == ropeHeldByEntityId)
			{
				p = sys.LastPoint;
			}
			p?.PinTo(entityItem, new Vec3f(entityItem.SelectionBox.X2 / 2f, entityItem.SelectionBox.Y2 / 2f, entityItem.SelectionBox.Z2 / 2f));
		}
	}

	public override void OnCollected(ItemStack stack, Entity entity)
	{
		int clothId = stack.Attributes.GetInt("clothId");
		if (clothId == 0)
		{
			return;
		}
		ClothSystem sys = cm.GetClothSystem(clothId);
		if (sys == null)
		{
			return;
		}
		ClothPoint p = null;
		if (sys.FirstPoint.PinnedToEntity is EntityItem { Alive: false })
		{
			p = sys.FirstPoint;
		}
		if (sys.LastPoint.PinnedToEntity is EntityItem { Alive: false })
		{
			p = sys.LastPoint;
		}
		if (p == null)
		{
			return;
		}
		Vec3d aheadPos = new Vec3d(0.0, entity.LocalEyePos.Y - 0.30000001192092896, 0.0).AheadCopy(0.10000000149011612, entity.SidedPos.Pitch, entity.SidedPos.Yaw).AheadCopy(0.4000000059604645, entity.SidedPos.Pitch, entity.SidedPos.Yaw - (float)Math.PI / 2f);
		p.PinTo(entity, aheadPos.ToVec3f());
		ItemSlot collectedSlot = null;
		(entity as EntityPlayer).WalkInventory(delegate(ItemSlot slot)
		{
			if (!slot.Empty && slot.Itemstack.Attributes.GetInt("clothId") == clothId)
			{
				collectedSlot = slot;
				return false;
			}
			return true;
		});
		if (sys.FirstPoint.PinnedToEntity == entity && sys.LastPoint.PinnedToEntity == entity)
		{
			sys.FirstPoint.UnPin();
			sys.LastPoint.UnPin();
			if (collectedSlot != null)
			{
				collectedSlot.Itemstack = null;
				collectedSlot.MarkDirty();
			}
			cm.UnregisterCloth(sys.ClothId);
		}
		else
		{
			collectedSlot?.Itemstack?.Attributes.SetLong("ropeHeldByEntityId", entity.EntityId);
			collectedSlot?.MarkDirty();
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
	}
}
