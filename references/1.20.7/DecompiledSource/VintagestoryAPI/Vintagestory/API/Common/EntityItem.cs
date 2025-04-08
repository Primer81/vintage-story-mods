using System;
using System.IO;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class EntityItem : Entity
{
	public EntityItemSlot Slot;

	public long itemSpawnedMilliseconds;

	private long lastPlayedSizzlesTotalMs;

	private float getWindSpeedAccum = 0.25f;

	private Vec3d windSpeed = new Vec3d();

	private BlockPos tmpPos = new BlockPos();

	private float windLoss;

	private float fireDamage;

	/// <summary>
	/// The itemstack attached to this Item Entity.
	/// </summary>
	public ItemStack Itemstack
	{
		get
		{
			return WatchedAttributes.GetItemstack("itemstack");
		}
		set
		{
			WatchedAttributes.SetItemstack("itemstack", value);
			Slot.Itemstack = value;
		}
	}

	/// <summary>
	/// The UID of the player that dropped this itemstack.
	/// </summary>
	public string ByPlayerUid
	{
		get
		{
			return WatchedAttributes.GetString("byPlayerUid");
		}
		set
		{
			WatchedAttributes.SetString("byPlayerUid", value);
		}
	}

	/// <summary>
	/// Returns the material density of the item.
	/// </summary>
	public override float MaterialDensity => (Slot.Itemstack?.Collectible != null) ? Slot.Itemstack.Collectible.MaterialDensity : 2000;

	/// <summary>
	/// Whether or not the EntityItem is interactable.
	/// </summary>
	public override bool IsInteractable => false;

	/// <summary>
	/// Get the HSV colors for the lighting.
	/// </summary>
	public override byte[] LightHsv => Slot.Itemstack?.Collectible?.GetLightHsv(World.BlockAccessor, null, Slot.Itemstack);

	public override double SwimmingOffsetY => base.SwimmingOffsetY;

	public EntityItem()
		: base(GlobalConstants.DefaultSimulationRange * 3 / 4)
	{
		Stats = new EntityStats(this);
		Slot = new EntityItemSlot(this);
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
	{
		World = api.World;
		Api = api;
		base.Properties = properties;
		Class = properties.Class;
		InChunkIndex3d = chunkindex3d;
		if (Itemstack == null || Itemstack.StackSize == 0 || !Itemstack.ResolveBlockOrItem(World))
		{
			Die();
			Itemstack = null;
			return;
		}
		alive = WatchedAttributes.GetInt("entityDead") == 0;
		WatchedAttributes.RegisterModifiedListener("onFire", base.updateOnFire);
		if (base.Properties.CollisionBoxSize != null || properties.SelectionBoxSize != null)
		{
			updateColSelBoxes();
		}
		DoInitialActiveCheck(api);
		base.Properties.Initialize(this, api);
		LocalEyePos.Y = base.Properties.EyeHeight;
		TriggerOnInitialized();
		WatchedAttributes.RegisterModifiedListener("itemstack", delegate
		{
			if (Itemstack != null && Itemstack.Collectible == null)
			{
				Itemstack.ResolveBlockOrItem(World);
			}
			Slot.Itemstack = Itemstack;
		});
		JsonObject gravityFactor = Itemstack.Collectible.Attributes?["gravityFactor"];
		if (gravityFactor != null && gravityFactor.Exists)
		{
			WatchedAttributes.SetDouble("gravityFactor", gravityFactor.AsDouble(1.0));
		}
		JsonObject airdragFactor = Itemstack.Collectible.Attributes?["airDragFactor"];
		if (airdragFactor != null && airdragFactor.Exists)
		{
			WatchedAttributes.SetDouble("airDragFactor", airdragFactor.AsDouble(1.0));
		}
		itemSpawnedMilliseconds = World.ElapsedMilliseconds;
		Swimming = (FeetInLiquid = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2).IsLiquid());
		tmpPos.Set(Pos.XInt, Pos.YInt, Pos.ZInt);
		windLoss = (float)World.BlockAccessor.GetDistanceToRainFall(tmpPos) / 4f;
	}

	public override void OnGameTick(float dt)
	{
		if (World.Side == EnumAppSide.Client)
		{
			try
			{
				base.OnGameTick(dt);
			}
			catch (Exception e)
			{
				if (World == null)
				{
					throw new NullReferenceException("'World' was null for EntityItem; entity is " + (alive ? "alive" : "post-lifetime"));
				}
				Api.Logger.Error("Erroring EntityItem tick: please report this as a bug!");
				Api.Logger.Error(e);
			}
		}
		else
		{
			foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
			{
				behavior.OnGameTick(dt);
			}
			if (InLava)
			{
				Ignite();
			}
			if (base.IsOnFire)
			{
				Block fluidBlock = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2);
				if ((fluidBlock.IsLiquid() && fluidBlock.LiquidCode != "lava") || World.ElapsedMilliseconds - OnFireBeginTotalMs > 12000)
				{
					base.IsOnFire = false;
				}
				else
				{
					ApplyFireDamage(dt);
					if (!alive && InLava)
					{
						DieInLava();
					}
				}
			}
		}
		if (!Alive)
		{
			return;
		}
		if (Itemstack != null)
		{
			if (!base.Collided && !Swimming && World.Side == EnumAppSide.Server)
			{
				getWindSpeedAccum += dt;
				if ((double)getWindSpeedAccum > 0.25)
				{
					getWindSpeedAccum = 0f;
					tmpPos.Set(Pos.XInt, Pos.YInt, Pos.ZInt);
					windSpeed = World.BlockAccessor.GetWindSpeedAt(tmpPos);
					windSpeed.X = Math.Max(0.0, Math.Abs(windSpeed.X) - (double)windLoss) * (double)Math.Sign(windSpeed.X);
					windSpeed.Y = Math.Max(0.0, Math.Abs(windSpeed.Y) - (double)windLoss) * (double)Math.Sign(windSpeed.Y);
					windSpeed.Z = Math.Max(0.0, Math.Abs(windSpeed.Z) - (double)windLoss) * (double)Math.Sign(windSpeed.Z);
				}
				float fac = GameMath.Clamp(1000f / (float)Itemstack.Collectible.MaterialDensity, 1f, 10f);
				base.SidedPos.Motion.X += windSpeed.X / 1000.0 * (double)fac * GameMath.Clamp(1.0 / (1.0 + Math.Abs(base.SidedPos.Motion.X)), 0.0, 1.0);
				base.SidedPos.Motion.Y += windSpeed.Y / 1000.0 * (double)fac * GameMath.Clamp(1.0 / (1.0 + Math.Abs(base.SidedPos.Motion.Y)), 0.0, 1.0);
				base.SidedPos.Motion.Z += windSpeed.Z / 1000.0 * (double)fac * GameMath.Clamp(1.0 / (1.0 + Math.Abs(base.SidedPos.Motion.Z)), 0.0, 1.0);
			}
			Itemstack.Collectible.OnGroundIdle(this);
			if (FeetInLiquid && !InLava)
			{
				float temp = Itemstack.Collectible.GetTemperature(World, Itemstack);
				if (temp > 20f)
				{
					Itemstack.Collectible.SetTemperature(World, Itemstack, Math.Max(20f, temp - 5f));
					if (temp > 90f)
					{
						double width = SelectionBox.XSize;
						Entity.SplashParticleProps.BasePos.Set(Pos.X - width / 2.0, Pos.Y - 0.75, Pos.Z - width / 2.0);
						Entity.SplashParticleProps.AddVelocity.Set(0f, 0f, 0f);
						Entity.SplashParticleProps.QuantityMul = 0.1f;
						World.SpawnParticles(Entity.SplashParticleProps);
					}
					if (temp > 200f && World.Side == EnumAppSide.Client && World.ElapsedMilliseconds - lastPlayedSizzlesTotalMs > 10000)
					{
						World.PlaySoundAt(new AssetLocation("sounds/sizzle"), this);
						lastPlayedSizzlesTotalMs = World.ElapsedMilliseconds;
					}
				}
			}
		}
		else
		{
			Die();
		}
		World.FrameProfiler.Mark("entity-tick-droppeditems");
	}

	public override void Ignite()
	{
		ItemStack stack = Itemstack;
		if (InLava || (stack != null && stack.Collectible.CombustibleProps != null && (stack.Collectible.CombustibleProps.MeltingPoint < 700 || stack.Collectible.CombustibleProps.BurnTemperature > 0)))
		{
			base.Ignite();
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		if (base.SidedProperties == null)
		{
			return;
		}
		foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
		{
			behavior.OnEntityDespawn(despawn);
		}
		WatchedAttributes.OnModified.Clear();
	}

	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
	}

	public override void UpdateDebugAttributes()
	{
	}

	public override void StartAnimation(string code)
	{
	}

	public override void StopAnimation(string code)
	{
	}

	public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
	{
		if (Alive)
		{
			Alive = false;
			DespawnReason = new EntityDespawnData
			{
				Reason = reason,
				DamageSourceForDeath = damageSourceForDeath
			};
		}
	}

	/// <summary>
	/// Builds and spawns an EntityItem from a provided ItemStack.
	/// </summary>
	/// <param name="itemstack">The contents of the EntityItem</param>
	/// <param name="position">The position of the EntityItem</param>
	/// <param name="velocity">The velocity of the EntityItem</param>
	/// <param name="world">The world the EntityItems preside in.</param>
	/// <returns>A freshly baked EntityItem to introduce to the world.</returns>
	public static EntityItem FromItemstack(ItemStack itemstack, Vec3d position, Vec3d velocity, IWorldAccessor world)
	{
		EntityItem item = new EntityItem();
		item.Code = GlobalConstants.EntityItemTypeCode;
		item.SimulationRange = (int)(0.75f * (float)GlobalConstants.DefaultSimulationRange);
		item.Itemstack = itemstack;
		item.ServerPos.SetPosWithDimension(position);
		if (velocity == null)
		{
			velocity = new Vec3d((float)world.Rand.NextDouble() * 0.1f - 0.05f, (float)world.Rand.NextDouble() * 0.1f - 0.05f, (float)world.Rand.NextDouble() * 0.1f - 0.05f);
		}
		item.ServerPos.Motion = velocity;
		item.Pos.SetFrom(item.ServerPos);
		return item;
	}

	public override bool CanCollect(Entity byEntity)
	{
		if (Alive)
		{
			return World.ElapsedMilliseconds - itemSpawnedMilliseconds > 1000;
		}
		return false;
	}

	public override ItemStack OnCollected(Entity byEntity)
	{
		return Slot.Itemstack;
	}

	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		return false;
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if (damageSource.Source == EnumDamageSource.Internal && damageSource.Type == EnumDamageType.Fire)
		{
			fireDamage += damage;
		}
		if (fireDamage > 4f)
		{
			Die();
		}
		return base.ReceiveDamage(damageSource, damage);
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		if (Itemstack != null)
		{
			Slot.Itemstack = Itemstack;
		}
		if (World != null)
		{
			ItemStack itemstack = Slot.Itemstack;
			if (itemstack == null || !itemstack.ResolveBlockOrItem(World))
			{
				Itemstack = null;
				Die();
			}
		}
	}
}
