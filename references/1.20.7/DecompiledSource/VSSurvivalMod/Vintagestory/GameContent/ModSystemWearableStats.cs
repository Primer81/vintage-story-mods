using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemWearableStats : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	private Dictionary<int, EnumCharacterDressType[]> clothingDamageTargetsByAttackTacket = new Dictionary<int, EnumCharacterDressType[]>
	{
		{
			0,
			new EnumCharacterDressType[3]
			{
				EnumCharacterDressType.Head,
				EnumCharacterDressType.Face,
				EnumCharacterDressType.Neck
			}
		},
		{
			1,
			new EnumCharacterDressType[5]
			{
				EnumCharacterDressType.UpperBody,
				EnumCharacterDressType.UpperBodyOver,
				EnumCharacterDressType.Shoulder,
				EnumCharacterDressType.Arm,
				EnumCharacterDressType.Hand
			}
		},
		{
			2,
			new EnumCharacterDressType[2]
			{
				EnumCharacterDressType.LowerBody,
				EnumCharacterDressType.Foot
			}
		}
	};

	private AssetLocation ripSound = new AssetLocation("sounds/effect/clothrip");

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		api.Event.LevelFinalize += Event_LevelFinalize;
		capi = api;
	}

	private void Event_LevelFinalize()
	{
		capi.World.Player.Entity.OnFootStep += delegate
		{
			onFootStep(capi.World.Player.Entity);
		};
		capi.World.Player.Entity.OnImpact += delegate(double motionY)
		{
			onFallToGround(capi.World.Player.Entity, motionY);
		};
		EntityBehaviorHealth bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorHealth>();
		if (bh != null)
		{
			bh.onDamaged += (float dmg, DamageSource dmgSource) => handleDamaged(capi.World.Player, dmg, dmgSource);
		}
		capi.Logger.VerboseDebug("Done wearable stats");
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		api.Event.PlayerJoin += Event_PlayerJoin;
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		IInventory inv = byPlayer.InventoryManager.GetOwnInventory("character");
		inv.SlotModified += delegate
		{
			updateWearableStats(inv, byPlayer);
		};
		EntityBehaviorHealth bh = byPlayer.Entity.GetBehavior<EntityBehaviorHealth>();
		if (bh != null)
		{
			bh.onDamaged += (float dmg, DamageSource dmgSource) => handleDamaged(byPlayer, dmg, dmgSource);
		}
		byPlayer.Entity.OnFootStep += delegate
		{
			onFootStep(byPlayer.Entity);
		};
		byPlayer.Entity.OnImpact += delegate(double motionY)
		{
			onFallToGround(byPlayer.Entity, motionY);
		};
		updateWearableStats(inv, byPlayer);
	}

	private void onFallToGround(EntityPlayer entity, double motionY)
	{
		if (Math.Abs(motionY) > 0.1)
		{
			onFootStep(entity);
		}
	}

	private void onFootStep(EntityPlayer entity)
	{
		InventoryBase inv = entity.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;
		if (inv == null)
		{
			return;
		}
		foreach (ItemSlot slot in inv)
		{
			if (!slot.Empty && slot.Itemstack.Collectible is ItemWearable { FootStepSounds: { } soundlocs } && soundlocs.Length != 0)
			{
				AssetLocation loc = soundlocs[api.World.Rand.Next(soundlocs.Length)];
				float pitch = (float)api.World.Rand.NextDouble() * 0.5f + 0.7f;
				float volume = (entity.Player.Entity.Controls.Sneak ? 0.5f : (1f + (float)api.World.Rand.NextDouble() * 0.3f + 0.7f));
				api.World.PlaySoundAt(loc, entity, (api.Side == EnumAppSide.Server) ? entity.Player : null, pitch, 16f, volume);
			}
		}
	}

	private float handleDamaged(IPlayer player, float damage, DamageSource dmgSource)
	{
		EnumDamageType type = dmgSource.Type;
		damage = applyShieldProtection(player, damage, dmgSource);
		if (damage <= 0f)
		{
			return 0f;
		}
		if (api.Side == EnumAppSide.Client)
		{
			return damage;
		}
		if (type != EnumDamageType.BluntAttack && type != EnumDamageType.PiercingAttack && type != EnumDamageType.SlashingAttack)
		{
			return damage;
		}
		if (dmgSource.Source == EnumDamageSource.Internal || dmgSource.Source == EnumDamageSource.Suicide)
		{
			return damage;
		}
		IInventory inv = player.InventoryManager.GetOwnInventory("character");
		double rnd = api.World.Rand.NextDouble();
		ItemSlot armorSlot;
		int attackTarget;
		if ((rnd -= 0.2) < 0.0)
		{
			armorSlot = inv[12];
			attackTarget = 0;
		}
		else if ((rnd -= 0.5) < 0.0)
		{
			armorSlot = inv[13];
			attackTarget = 1;
		}
		else
		{
			armorSlot = inv[14];
			attackTarget = 2;
		}
		if (armorSlot.Empty || !(armorSlot.Itemstack.Item is ItemWearable) || armorSlot.Itemstack.Collectible.GetRemainingDurability(armorSlot.Itemstack) <= 0)
		{
			EnumCharacterDressType[] dressTargets = clothingDamageTargetsByAttackTacket[attackTarget];
			EnumCharacterDressType target = dressTargets[api.World.Rand.Next(dressTargets.Length)];
			ItemSlot targetslot = inv[(int)target];
			if (!targetslot.Empty)
			{
				float mul = 0.25f;
				if (type == EnumDamageType.SlashingAttack)
				{
					mul = 1f;
				}
				if (type == EnumDamageType.PiercingAttack)
				{
					mul = 0.5f;
				}
				float diff = (0f - damage) / 100f * mul;
				if ((double)Math.Abs(diff) > 0.05)
				{
					api.World.PlaySoundAt(ripSound, player.Entity);
				}
				(targetslot.Itemstack.Collectible as ItemWearable)?.ChangeCondition(targetslot, diff);
			}
			return damage;
		}
		ProtectionModifiers protMods = (armorSlot.Itemstack.Item as ItemWearable).ProtectionModifiers;
		int weaponTier = dmgSource.DamageTier;
		float flatDmgProt = protMods.FlatDamageReduction;
		float percentProt = protMods.RelativeProtection;
		for (int tier = 1; tier <= weaponTier; tier++)
		{
			bool num = tier > protMods.ProtectionTier;
			float flatLoss = (num ? protMods.PerTierFlatDamageReductionLoss[1] : protMods.PerTierFlatDamageReductionLoss[0]);
			float percLoss = (num ? protMods.PerTierRelativeProtectionLoss[1] : protMods.PerTierRelativeProtectionLoss[0]);
			if (num && protMods.HighDamageTierResistant)
			{
				flatLoss /= 2f;
				percLoss /= 2f;
			}
			flatDmgProt -= flatLoss;
			percentProt *= 1f - percLoss;
		}
		float durabilityLoss = 0.5f + damage * Math.Max(0.5f, (weaponTier - protMods.ProtectionTier) * 3);
		int durabilityLossInt = GameMath.RoundRandom(api.World.Rand, durabilityLoss);
		damage = Math.Max(0f, damage - flatDmgProt);
		damage *= 1f - Math.Max(0f, percentProt);
		armorSlot.Itemstack.Collectible.DamageItem(api.World, player.Entity, armorSlot, durabilityLossInt);
		if (armorSlot.Empty)
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
		}
		return damage;
	}

	private float applyShieldProtection(IPlayer player, float damage, DamageSource dmgSource)
	{
		double horizontalAngleProtectionRange = 1.0471975803375244;
		float unabsorbedDamage = damage;
		ItemSlot[] shieldSlots = new ItemSlot[2]
		{
			player.Entity.LeftHandItemSlot,
			player.Entity.RightHandItemSlot
		};
		for (int i = 0; i < shieldSlots.Length; i++)
		{
			ItemSlot shieldSlot = shieldSlots[i];
			JsonObject attr = shieldSlot.Itemstack?.ItemAttributes?["shield"];
			if (attr == null || !attr.Exists)
			{
				continue;
			}
			bool projectile = (dmgSource.SourceEntity?.Properties.Attributes?["isProjectile"].AsBool()).GetValueOrDefault();
			string usetype = ((player.Entity.Controls.Sneak && player.Entity.Attributes.GetInt("aiming") != 1) ? "active" : "passive");
			float flatdmgabsorb = 0f;
			float chance = 0f;
			if (projectile && attr["protectionChance"][usetype + "-projectile"].Exists)
			{
				chance = attr["protectionChance"][usetype + "-projectile"].AsFloat();
				flatdmgabsorb = attr["projectileDamageAbsorption"].AsFloat(2f);
			}
			else
			{
				chance = attr["protectionChance"][usetype].AsFloat();
				flatdmgabsorb = attr["damageAbsorption"].AsFloat(2f);
			}
			if (!dmgSource.GetAttackAngle(player.Entity.Pos.XYZ, out var attackYaw, out var attackPitch))
			{
				break;
			}
			bool verticalAttack = Math.Abs(attackPitch) > 1.1344640254974365;
			double playerYaw = player.Entity.Pos.Yaw;
			double playerPitch = player.Entity.Pos.Pitch;
			if (projectile)
			{
				double x = dmgSource.SourceEntity.SidedPos.Motion.X;
				double dy = dmgSource.SourceEntity.SidedPos.Motion.Y;
				double dz = dmgSource.SourceEntity.SidedPos.Motion.Z;
				verticalAttack = Math.Sqrt(x * x + dz * dz) < Math.Abs(dy);
			}
			if ((!verticalAttack) ? ((double)Math.Abs(GameMath.AngleRadDistance((float)playerYaw, (float)attackYaw)) < horizontalAngleProtectionRange) : (Math.Abs(GameMath.AngleRadDistance((float)playerPitch, (float)attackPitch)) < (float)Math.PI / 6f))
			{
				float totaldmgabsorb = 0f;
				double rndval = api.World.Rand.NextDouble();
				if (rndval < (double)chance)
				{
					totaldmgabsorb += flatdmgabsorb;
				}
				(player as IServerPlayer)?.SendMessage(GlobalConstants.DamageLogChatGroup, Lang.Get("{0:0.#} of {1:0.#} damage blocked by shield ({2} use)", Math.Min(totaldmgabsorb, damage), damage, usetype), EnumChatType.Notification);
				damage = Math.Max(0f, damage - totaldmgabsorb);
				string key = "blockSound" + ((unabsorbedDamage > 6f) ? "Heavy" : "Light");
				AssetLocation sloc = AssetLocation.Create(shieldSlot.Itemstack.ItemAttributes["shield"][key].AsString("held/shieldblock-wood-light"), shieldSlot.Itemstack.Collectible.Code.Domain).WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg");
				api.World.PlaySoundAt(sloc, player);
				if (rndval < (double)chance)
				{
					(api as ICoreServerAPI).Network.BroadcastEntityPacket(player.Entity.EntityId, 200, SerializerUtil.Serialize("shieldBlock" + ((i == 0) ? "L" : "R")));
				}
				if (api.Side == EnumAppSide.Server)
				{
					shieldSlot.Itemstack.Collectible.DamageItem(api.World, dmgSource.SourceEntity, shieldSlot);
					shieldSlot.MarkDirty();
				}
			}
		}
		return damage;
	}

	private void updateWearableStats(IInventory inv, IServerPlayer player)
	{
		StatModifiers allmod = new StatModifiers();
		float walkSpeedmul = player.Entity.Stats.GetBlended("armorWalkSpeedAffectedness");
		foreach (ItemSlot slot in inv)
		{
			if (slot.Empty || !(slot.Itemstack.Item is ItemWearable))
			{
				continue;
			}
			StatModifiers statmod = (slot.Itemstack.Item as ItemWearable).StatModifers;
			if (statmod != null)
			{
				bool broken = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) == 0;
				allmod.canEat &= statmod.canEat;
				allmod.healingeffectivness += (broken ? Math.Min(0f, statmod.healingeffectivness) : statmod.healingeffectivness);
				allmod.hungerrate += (broken ? Math.Max(0f, statmod.hungerrate) : statmod.hungerrate);
				if (statmod.walkSpeed < 0f)
				{
					allmod.walkSpeed += statmod.walkSpeed * walkSpeedmul;
				}
				else
				{
					allmod.walkSpeed += (broken ? 0f : statmod.walkSpeed);
				}
				allmod.rangedWeaponsAcc += (broken ? Math.Min(0f, statmod.rangedWeaponsAcc) : statmod.rangedWeaponsAcc);
				allmod.rangedWeaponsSpeed += (broken ? Math.Min(0f, statmod.rangedWeaponsSpeed) : statmod.rangedWeaponsSpeed);
			}
		}
		EntityPlayer entity = player.Entity;
		entity.Stats.Set("walkspeed", "wearablemod", allmod.walkSpeed, persistent: true).Set("healingeffectivness", "wearablemod", allmod.healingeffectivness, persistent: true).Set("hungerrate", "wearablemod", allmod.hungerrate, persistent: true)
			.Set("rangedWeaponsAcc", "wearablemod", allmod.rangedWeaponsAcc, persistent: true)
			.Set("rangedWeaponsSpeed", "wearablemod", allmod.rangedWeaponsSpeed, persistent: true);
		entity.walkSpeed = entity.Stats.GetBlended("walkspeed");
		entity.WatchedAttributes.SetBool("canEat", allmod.canEat);
	}
}
