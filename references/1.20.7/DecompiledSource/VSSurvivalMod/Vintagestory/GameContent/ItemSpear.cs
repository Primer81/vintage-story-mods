using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSpear : Item
{
	private bool isHackingSpear;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CollectibleBehaviorAnimationAuthoritative bh = GetCollectibleBehavior<CollectibleBehaviorAnimationAuthoritative>(withInheritance: true);
		if (bh == null)
		{
			api.World.Logger.Warning("Spear {0} uses ItemSpear class, but lacks required AnimationAuthoritative behavior. I'll take the freedom to add this behavior, but please fix json item type.", Code);
			bh = new CollectibleBehaviorAnimationAuthoritative(this);
			bh.OnLoaded(api);
			CollectibleBehaviors = CollectibleBehaviors.Append(bh);
		}
		bh.OnBeginHitEntity += ItemSpear_OnBeginHitEntity;
		isHackingSpear = Attributes.IsTrue("hacking");
	}

	private void ItemSpear_OnBeginHitEntity(EntityAgent byEntity, ref EnumHandling handling)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		EntitySelection entitySel = (byEntity as EntityPlayer)?.EntitySelection;
		if (byEntity.Attributes.GetInt("didattack") == 0 && entitySel != null)
		{
			byEntity.Attributes.SetInt("didattack", 1);
			_ = byEntity.ActiveHandItemSlot;
			JsonObject attributes = entitySel.Entity.Properties.Attributes;
			bool canhackEntity = attributes != null && attributes["hackedEntity"].Exists && isHackingSpear && api.ModLoader.GetModSystem<CharacterSystem>().HasTrait((byEntity as EntityPlayer).Player, "technical");
			ICoreServerAPI sapi = api as ICoreServerAPI;
			if (canhackEntity)
			{
				sapi.World.PlaySoundAt(new AssetLocation("sounds/player/hackingspearhit.ogg"), entitySel.Entity);
			}
			if (api.World.Rand.NextDouble() < 0.15 && canhackEntity)
			{
				SpawnEntityInPlaceOf(entitySel.Entity, entitySel.Entity.Properties.Attributes["hackedEntity"].AsString(), byEntity);
				sapi.World.DespawnEntity(entitySel.Entity, new EntityDespawnData
				{
					Reason = EnumDespawnReason.Removed
				});
			}
		}
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
	{
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling != EnumHandHandling.PreventDefault)
		{
			handling = EnumHandHandling.PreventDefault;
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.StartAnimation("aim");
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.Attributes.SetInt("aiming", 0);
		byEntity.StopAnimation("aim");
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
		byEntity.StopAnimation("aim");
		if (secondsUsed < 0.35f)
		{
			return;
		}
		float damage = 1.5f;
		if (slot.Itemstack.Collectible.Attributes != null)
		{
			damage = slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
		}
		(api as ICoreClientAPI)?.World.AddCameraShake(0.17f);
		ItemStack stack = slot.TakeOut(1);
		slot.MarkDirty();
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, byPlayer, randomizePitch: false, 8f);
		EntityProperties type = byEntity.World.GetEntityType(new AssetLocation(Attributes["spearEntityCode"].AsString()));
		EntityProjectile enpr = byEntity.World.ClassRegistry.CreateEntity(type) as EntityProjectile;
		enpr.FiredBy = byEntity;
		enpr.Damage = damage;
		enpr.DamageTier = Attributes["damageTier"].AsInt();
		enpr.ProjectileStack = stack;
		enpr.DropOnImpactChance = 1.1f;
		enpr.DamageStackOnImpact = true;
		enpr.Weight = 0.3f;
		float acc = 1f - byEntity.Attributes.GetFloat("aimingAccuracy");
		double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)acc * 0.75;
		double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)acc * 0.75;
		Vec3d pos = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.2, 0.0);
		Vec3d velocity = (pos.AheadCopy(1.0, (double)byEntity.ServerPos.Pitch + rndpitch, (double)byEntity.ServerPos.Yaw + rndyaw) - pos) * 0.65 * byEntity.Stats.GetBlended("bowDrawingStrength");
		Vec3d spawnPos = byEntity.ServerPos.BehindCopy(0.15).XYZ.Add(byEntity.LocalEyePos.X, byEntity.LocalEyePos.Y - 0.2, byEntity.LocalEyePos.Z);
		enpr.ServerPos.SetPosWithDimension(spawnPos);
		enpr.ServerPos.Motion.Set(velocity);
		enpr.Pos.SetFrom(enpr.ServerPos);
		enpr.World = byEntity.World;
		enpr.SetRotation();
		byEntity.World.SpawnEntity(enpr);
		byEntity.StartAnimation("throw");
		if (byEntity is EntityPlayer)
		{
			RefillSlotIfEmpty(slot, byEntity, (ItemStack itemstack) => itemstack.Collectible is ItemSpear);
		}
		float pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;
		byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/strike"), byPlayer.Entity, byPlayer, pitch * 0.9f + (float)api.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);
	}

	private void SpawnEntityInPlaceOf(Entity byEntity, string code, EntityAgent causingEntity)
	{
		AssetLocation location = AssetLocation.Create(code, byEntity.Code.Domain);
		EntityProperties type = byEntity.World.GetEntityType(location);
		if (type == null)
		{
			byEntity.World.Logger.Error("ItemCreature: No such entity - {0}", location);
			if (api.World.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", $"No such entity loaded - '{location}'.");
			}
			return;
		}
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(type);
		if (entity != null)
		{
			entity.ServerPos.X = byEntity.ServerPos.X;
			entity.ServerPos.Y = byEntity.ServerPos.Y;
			entity.ServerPos.Z = byEntity.ServerPos.Z;
			entity.ServerPos.Motion.X = byEntity.ServerPos.Motion.X;
			entity.ServerPos.Motion.Y = byEntity.ServerPos.Motion.Y;
			entity.ServerPos.Motion.Z = byEntity.ServerPos.Motion.Z;
			entity.ServerPos.Yaw = byEntity.ServerPos.Yaw;
			entity.Pos.SetFrom(entity.ServerPos);
			entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			entity.Attributes.SetString("origin", "playerplaced");
			entity.WatchedAttributes.SetLong("guardedEntityId", byEntity.EntityId);
			if (causingEntity is EntityPlayer eplr)
			{
				entity.WatchedAttributes.SetString("guardedPlayerUid", eplr.PlayerUID);
			}
			byEntity.World.SpawnEntity(entity);
		}
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			float damage = 1.5f;
			if (inSlot.Itemstack.Collectible.Attributes != null)
			{
				damage = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			}
			dsc.AppendLine(damage + Lang.Get("piercing-damage-thrown"));
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-throw",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
