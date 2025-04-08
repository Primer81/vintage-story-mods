using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemBow : Item
{
	private WorldInteraction[] interactions;

	private string aimAnimation;

	public override void OnLoaded(ICoreAPI api)
	{
		aimAnimation = Attributes["aimAnimation"].AsString();
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "bowInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current.Code.PathStartsWith("arrow-"))
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-chargebow",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "dropitems",
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	protected ItemSlot GetNextArrow(EntityAgent byEntity)
	{
		ItemSlot slot = null;
		byEntity.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative)
			{
				return true;
			}
			ItemStack itemstack = invslot.Itemstack;
			if (itemstack != null && itemstack.Collectible != null && itemstack.Collectible.Code.PathStartsWith("arrow-") && itemstack.StackSize > 0)
			{
				slot = invslot;
				return false;
			}
			return true;
		});
		return slot;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling != EnumHandHandling.PreventDefault && GetNextArrow(byEntity) != null)
		{
			if (byEntity.World is IClientWorldAccessor)
			{
				slot.Itemstack.TempAttributes.SetInt("renderVariant", 1);
			}
			slot.Itemstack.Attributes.SetInt("renderVariant", 1);
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.AnimManager.StartAnimation(aimAnimation);
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/bow-draw"), byEntity, byPlayer, randomizePitch: false, 8f);
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		int renderVariant = GameMath.Clamp((int)Math.Ceiling(secondsUsed * 4f), 0, 3);
		int @int = slot.Itemstack.Attributes.GetInt("renderVariant");
		slot.Itemstack.TempAttributes.SetInt("renderVariant", renderVariant);
		slot.Itemstack.Attributes.SetInt("renderVariant", renderVariant);
		if (@int != renderVariant)
		{
			(byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.AnimManager.StopAnimation(aimAnimation);
		if (byEntity.World is IClientWorldAccessor)
		{
			slot.Itemstack?.TempAttributes.RemoveAttribute("renderVariant");
		}
		slot.Itemstack?.Attributes.SetInt("renderVariant", 0);
		if (cancelReason != EnumItemUseCancelReason.Destroyed)
		{
			(byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
		}
		if (cancelReason != 0)
		{
			byEntity.Attributes.SetInt("aimingCancel", 1);
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.Attributes.GetInt("aimingCancel") == 1)
		{
			return;
		}
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.AnimManager.StopAnimation(aimAnimation);
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
			byEntity.AnimManager.StartAnimation("bowhit");
			return;
		}
		slot.Itemstack.Attributes.SetInt("renderVariant", 0);
		(byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
		if (secondsUsed < 0.65f)
		{
			return;
		}
		ItemSlot arrowSlot = GetNextArrow(byEntity);
		if (arrowSlot != null)
		{
			float damage = 0f;
			if (slot.Itemstack.Collectible.Attributes != null)
			{
				damage += slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			}
			if (arrowSlot.Itemstack.Collectible.Attributes != null)
			{
				damage += arrowSlot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			}
			ItemStack stack = arrowSlot.TakeOut(1);
			arrowSlot.MarkDirty();
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/bow-release"), byEntity, null, randomizePitch: false, 8f);
			float breakChance = 0.5f;
			if (stack.ItemAttributes != null)
			{
				breakChance = stack.ItemAttributes["breakChanceOnImpact"].AsFloat(0.5f);
			}
			EntityProperties type = byEntity.World.GetEntityType(new AssetLocation(stack.ItemAttributes["arrowEntityCode"].AsString("arrow-" + stack.Collectible.Variant["material"])));
			EntityProjectile entityarrow = byEntity.World.ClassRegistry.CreateEntity(type) as EntityProjectile;
			entityarrow.FiredBy = byEntity;
			entityarrow.Damage = damage;
			entityarrow.DamageTier = Attributes["damageTier"].AsInt();
			entityarrow.ProjectileStack = stack;
			entityarrow.DropOnImpactChance = 1f - breakChance;
			float acc = Math.Max(0.001f, 1f - byEntity.Attributes.GetFloat("aimingAccuracy"));
			double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)acc * 0.75;
			double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)acc * 0.75;
			Vec3d pos = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
			Vec3d velocity = (pos.AheadCopy(1.0, (double)byEntity.SidedPos.Pitch + rndpitch, (double)byEntity.SidedPos.Yaw + rndyaw) - pos) * byEntity.Stats.GetBlended("bowDrawingStrength");
			entityarrow.ServerPos.SetPosWithDimension(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
			entityarrow.ServerPos.Motion.Set(velocity);
			entityarrow.Pos.SetFrom(entityarrow.ServerPos);
			entityarrow.World = byEntity.World;
			entityarrow.SetRotation();
			byEntity.World.SpawnEntity(entityarrow);
			slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);
			slot.MarkDirty();
			byEntity.AnimManager.StartAnimation("bowhit");
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			float dmg = inSlot.Itemstack.Collectible.Attributes?["damage"].AsFloat() ?? 0f;
			if (dmg != 0f)
			{
				dsc.AppendLine(Lang.Get("bow-piercingdamage", dmg));
			}
			float accuracyBonus = inSlot.Itemstack.Collectible?.Attributes["statModifier"]["rangedWeaponsAcc"].AsFloat() ?? 0f;
			if (accuracyBonus != 0f)
			{
				dsc.AppendLine(Lang.Get("bow-accuracybonus", (accuracyBonus > 0f) ? "+" : "", (int)(100f * accuracyBonus)));
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
