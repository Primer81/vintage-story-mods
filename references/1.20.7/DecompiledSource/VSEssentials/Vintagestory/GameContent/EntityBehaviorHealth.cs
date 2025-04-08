using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorHealth : EntityBehavior
{
	public const double FallDamageYMotionThreshold = -0.19;

	public const float FallDamageFallenDistanceThreshold = 3.5f;

	private float secondsSinceLastUpdate;

	[Obsolete("This is nullable. Please call SetMaxHealthModifiers() instead of writing to it directly.")]
	public Dictionary<string, float> MaxHealthModifiers;

	private ITreeAttribute healthTree => entity.WatchedAttributes.GetTreeAttribute("health");

	public float Health
	{
		get
		{
			return healthTree.GetFloat("currenthealth");
		}
		set
		{
			healthTree.SetFloat("currenthealth", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float BaseMaxHealth
	{
		get
		{
			return healthTree.GetFloat("basemaxhealth");
		}
		set
		{
			healthTree.SetFloat("basemaxhealth", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float MaxHealth
	{
		get
		{
			return healthTree.GetFloat("maxhealth");
		}
		set
		{
			healthTree.SetFloat("maxhealth", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public event OnDamagedDelegate onDamaged = (float dmg, DamageSource dmgSource) => dmg;

	public void SetMaxHealthModifiers(string key, float value)
	{
		bool dirty = true;
		float oldvalue;
		if (MaxHealthModifiers == null)
		{
			MaxHealthModifiers = new Dictionary<string, float>();
			if (value == 0f)
			{
				dirty = false;
			}
		}
		else if (MaxHealthModifiers.TryGetValue(key, out oldvalue) && oldvalue == value)
		{
			dirty = false;
		}
		MaxHealthModifiers[key] = value;
		if (dirty)
		{
			MarkDirty();
		}
	}

	public void MarkDirty()
	{
		UpdateMaxHealth();
		entity.WatchedAttributes.MarkPathDirty("health");
	}

	public void UpdateMaxHealth()
	{
		float totalMaxHealth = BaseMaxHealth;
		Dictionary<string, float> MaxHealthModifiers = this.MaxHealthModifiers;
		if (MaxHealthModifiers != null)
		{
			foreach (KeyValuePair<string, float> item in MaxHealthModifiers)
			{
				totalMaxHealth += item.Value;
			}
		}
		totalMaxHealth += entity.Stats.GetBlended("maxhealthExtraPoints") - 1f;
		bool num = Health >= MaxHealth;
		MaxHealth = totalMaxHealth;
		if (num)
		{
			Health = MaxHealth;
		}
	}

	public EntityBehaviorHealth(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
		if (healthTree == null)
		{
			entity.WatchedAttributes.SetAttribute("health", healthTree = new TreeAttribute());
			BaseMaxHealth = typeAttributes["maxhealth"].AsFloat(20f);
			Health = typeAttributes["currenthealth"].AsFloat(BaseMaxHealth);
			MarkDirty();
			return;
		}
		if (healthTree.GetFloat("basemaxhealth") == 0f)
		{
			BaseMaxHealth = typeAttributes["maxhealth"].AsFloat(20f);
			MarkDirty();
		}
		secondsSinceLastUpdate = (float)entity.World.Rand.NextDouble();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		if (entity.Pos.Y < -30.0)
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Void,
				Type = EnumDamageType.Gravity
			}, 4f);
		}
		secondsSinceLastUpdate += deltaTime;
		if (!(secondsSinceLastUpdate >= 1f))
		{
			return;
		}
		if (entity.Alive)
		{
			float health = Health;
			float maxHealth = MaxHealth;
			if (health < maxHealth)
			{
				float healthRegenSpeed = ((entity is EntityPlayer) ? entity.Api.World.Config.GetString("playerHealthRegenSpeed", "1").ToFloat() : entity.WatchedAttributes.GetFloat("regenSpeed", 1f));
				float healthRegenPerGameSecond = 0.000333333f * healthRegenSpeed;
				float multiplierPerGameSec = secondsSinceLastUpdate * entity.Api.World.Calendar.SpeedOfTime * entity.Api.World.Calendar.CalendarSpeedMul;
				if (entity is EntityPlayer plr)
				{
					EntityBehaviorHunger ebh = entity.GetBehavior<EntityBehaviorHunger>();
					if (ebh != null)
					{
						if (plr.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
						{
							return;
						}
						healthRegenPerGameSecond = GameMath.Clamp(healthRegenPerGameSecond * ebh.Saturation / ebh.MaxSaturation * 1f / 0.75f, 0f, healthRegenPerGameSecond);
						ebh.ConsumeSaturation(150f * multiplierPerGameSec * healthRegenPerGameSecond);
					}
				}
				Health = Math.Min(health + multiplierPerGameSec * healthRegenPerGameSecond, maxHealth);
			}
		}
		if (entity is EntityPlayer && entity.World.Side == EnumAppSide.Server)
		{
			int rainy = entity.World.BlockAccessor.GetRainMapHeightAt((int)entity.ServerPos.X, (int)entity.ServerPos.Z);
			if (entity.ServerPos.Y >= (double)rainy)
			{
				PrecipitationState state = entity.Api.ModLoader.GetModSystem<WeatherSystemBase>().GetPrecipitationState(entity.ServerPos.XYZ);
				if (state != null && state.ParticleSize >= 0.5 && state.Type == EnumPrecipitationType.Hail && entity.World.Rand.NextDouble() < state.Level / 2.0)
				{
					entity.ReceiveDamage(new DamageSource
					{
						Source = EnumDamageSource.Weather,
						Type = EnumDamageType.BluntAttack
					}, (float)state.ParticleSize / 15f);
				}
			}
		}
		secondsSinceLastUpdate = 0f;
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		float damageBeforeArmor = damage;
		if (this.onDamaged != null)
		{
			Delegate[] invocationList = this.onDamaged.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				OnDamagedDelegate dele = (OnDamagedDelegate)invocationList[i];
				damage = dele(damage, damageSource);
			}
		}
		if (damageSource.Type == EnumDamageType.Heal)
		{
			if (damageSource.Source != EnumDamageSource.Revive)
			{
				damage *= Math.Max(0f, entity.Stats.GetBlended("healingeffectivness"));
				Health = Math.Min(Health + damage, MaxHealth);
			}
			else
			{
				damage = Math.Min(damage, MaxHealth);
				damage *= Math.Max(0.33f, entity.Stats.GetBlended("healingeffectivness"));
				Health = damage;
			}
			entity.OnHurt(damageSource, damage);
			UpdateMaxHealth();
		}
		else
		{
			if (!entity.Alive || damage <= 0f)
			{
				return;
			}
			if (entity is EntityPlayer player && damageSource.GetCauseEntity() is EntityPlayer otherPlayer)
			{
				string weapon = ((damageSource.SourceEntity == otherPlayer) ? (otherPlayer.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code.ToString() ?? "hands") : damageSource.SourceEntity.Code.ToString());
				entity.Api.Logger.Audit("{0} at {1} got {2}/{3} damage {4} {5} by {6}", player.Player.PlayerName, entity.Pos.AsBlockPos, damage, damageBeforeArmor, damageSource.Type.ToString().ToLowerInvariant(), weapon, otherPlayer.GetName());
			}
			Health -= damage;
			entity.OnHurt(damageSource, damage);
			UpdateMaxHealth();
			if (Health <= 0f)
			{
				Health = 0f;
				entity.Die(EnumDespawnReason.Death, damageSource);
				return;
			}
			if (damage > 1f)
			{
				entity.AnimManager.StartAnimation("hurt");
			}
			if (damageSource.Type != EnumDamageType.Heal)
			{
				entity.PlayEntitySound("hurt");
			}
		}
	}

	public override void OnFallToGround(Vec3d positionBeforeFalling, double withYMotion)
	{
		if (!entity.Properties.FallDamage)
		{
			return;
		}
		bool gliding = (entity as EntityAgent)?.ServerControls.Gliding ?? false;
		double yDistance = Math.Abs(positionBeforeFalling.Y - entity.Pos.Y);
		if (yDistance < 3.5)
		{
			return;
		}
		if (gliding)
		{
			yDistance = Math.Min(yDistance / 2.0, Math.Min(14.0, yDistance));
			withYMotion /= 2.0;
			if ((double)entity.ServerPos.Pitch < 3.9269909262657166)
			{
				yDistance = 0.0;
			}
		}
		if (!(withYMotion > -0.19))
		{
			yDistance *= (double)entity.Properties.FallDamageMultiplier;
			double fallDamage = Math.Max(0.0, yDistance - 3.5);
			double expectedYMotion = -0.04100000113248825 * Math.Pow(fallDamage, 0.75) - 0.2199999988079071;
			double yMotionLoss = Math.Max(0.0, 0.0 - expectedYMotion + withYMotion);
			fallDamage -= 20.0 * yMotionLoss;
			if (!(fallDamage <= 0.0))
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Fall,
					Type = EnumDamageType.Gravity
				}, (float)fallDamage);
			}
		}
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		ICoreClientAPI obj = entity.Api as ICoreClientAPI;
		if (obj != null && (obj.World.Player?.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative)
		{
			infotext.AppendLine(Lang.Get("Health: {0}/{1}", Health, MaxHealth));
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		if (IsHealable(player.Entity))
		{
			return (player.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible as ICanHealCreature).GetHealInteractionHelp(world, es, player).Append(base.GetInteractionHelp(world, es, player, ref handled));
		}
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public bool IsHealable(EntityAgent eagent)
	{
		ICanHealCreature ichc = eagent.RightHandItemSlot?.Itemstack?.Collectible as ICanHealCreature;
		if (Health < MaxHealth)
		{
			return ichc?.CanHeal(entity) ?? false;
		}
		return false;
	}

	public override string PropertyName()
	{
		return "health";
	}
}
