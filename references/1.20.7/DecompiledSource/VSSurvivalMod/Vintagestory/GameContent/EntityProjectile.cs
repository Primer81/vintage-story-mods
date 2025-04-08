using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityProjectile : Entity
{
	protected bool beforeCollided;

	protected bool stuck;

	protected long msLaunch;

	protected long msCollide;

	protected Vec3d motionBeforeCollide = new Vec3d();

	protected CollisionTester collTester = new CollisionTester();

	protected Cuboidf collisionTestBox;

	protected EntityPartitioning ep;

	protected List<long> entitiesHit = new List<long>();

	protected long FiredByMountEntityId;

	public Entity FiredBy;

	public float Weight = 0.1f;

	public float Damage;

	public EnumDamageType DamageType = EnumDamageType.PiercingAttack;

	public int DamageTier;

	public ItemStack ProjectileStack;

	public float DropOnImpactChance;

	public bool DamageStackOnImpact;

	public bool EntityHit { get; protected set; }

	public bool NonCollectible
	{
		get
		{
			return Attributes.GetBool("nonCollectible");
		}
		set
		{
			Attributes.SetBool("nonCollectible", value);
		}
	}

	public override bool ApplyGravity => !stuck;

	public override bool IsInteractable => false;

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		if (Api.Side == EnumAppSide.Server && FiredBy != null)
		{
			WatchedAttributes.SetLong("firedBy", FiredBy.EntityId);
		}
		if (Api.Side == EnumAppSide.Client)
		{
			FiredBy = Api.World.GetEntityById(WatchedAttributes.GetLong("firedBy", 0L));
		}
		msLaunch = World.ElapsedMilliseconds;
		if (FiredBy != null && FiredBy is EntityAgent firedByAgent && firedByAgent.MountedOn?.Entity != null)
		{
			FiredByMountEntityId = firedByAgent.MountedOn.Entity.EntityId;
		}
		collisionTestBox = SelectionBox.Clone().OmniGrowBy(0.05f);
		GetBehavior<EntityBehaviorPassivePhysics>().OnPhysicsTickCallback = onPhysicsTickCallback;
		ep = api.ModLoader.GetModSystem<EntityPartitioning>();
		GetBehavior<EntityBehaviorPassivePhysics>().CollisionYExtra = 0f;
	}

	private void onPhysicsTickCallback(float dtFac)
	{
		if (ShouldDespawn || !Alive || World.ElapsedMilliseconds <= msCollide + 500)
		{
			return;
		}
		EntityPos pos = base.SidedPos;
		if (pos.Motion.LengthSq() < 0.04000000000000001)
		{
			return;
		}
		Cuboidd projectileBox = SelectionBox.ToDouble().Translate(pos.X, pos.Y, pos.Z);
		if (pos.Motion.X < 0.0)
		{
			projectileBox.X1 += pos.Motion.X * (double)dtFac;
		}
		else
		{
			projectileBox.X2 += pos.Motion.X * (double)dtFac;
		}
		if (pos.Motion.Y < 0.0)
		{
			projectileBox.Y1 += pos.Motion.Y * (double)dtFac;
		}
		else
		{
			projectileBox.Y2 += pos.Motion.Y * (double)dtFac;
		}
		if (pos.Motion.Z < 0.0)
		{
			projectileBox.Z1 += pos.Motion.Z * (double)dtFac;
		}
		else
		{
			projectileBox.Z2 += pos.Motion.Z * (double)dtFac;
		}
		ep.WalkEntities(pos.XYZ, 5.0, delegate(Entity e)
		{
			if (e.EntityId == EntityId || (FiredBy != null && e.EntityId == FiredBy.EntityId && World.ElapsedMilliseconds - msLaunch < 500) || !e.IsInteractable)
			{
				return true;
			}
			if (entitiesHit.Contains(e.EntityId))
			{
				return false;
			}
			if (e.EntityId == FiredByMountEntityId && World.ElapsedMilliseconds - msLaunch < 500)
			{
				return true;
			}
			if (e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).IntersectsOrTouches(projectileBox))
			{
				impactOnEntity(e);
				return false;
			}
			return true;
		}, EnumEntitySearchType.Creatures);
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (ShouldDespawn)
		{
			return;
		}
		EntityPos pos = base.SidedPos;
		stuck = base.Collided || collTester.IsColliding(World.BlockAccessor, collisionTestBox, pos.XYZ) || WatchedAttributes.GetBool("stuck");
		if (Api.Side == EnumAppSide.Server)
		{
			WatchedAttributes.SetBool("stuck", stuck);
		}
		double impactSpeed = Math.Max(motionBeforeCollide.Length(), pos.Motion.Length());
		if (stuck)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				ServerPos.SetFrom(Pos);
			}
			IsColliding(pos, impactSpeed);
			entitiesHit.Clear();
		}
		else
		{
			SetRotation();
			if (!TryAttackEntity(impactSpeed))
			{
				beforeCollided = false;
				motionBeforeCollide.Set(pos.Motion.X, pos.Motion.Y, pos.Motion.Z);
			}
		}
	}

	public override void OnCollided()
	{
		EntityPos pos = base.SidedPos;
		IsColliding(base.SidedPos, Math.Max(motionBeforeCollide.Length(), pos.Motion.Length()));
		motionBeforeCollide.Set(pos.Motion.X, pos.Motion.Y, pos.Motion.Z);
	}

	protected virtual void IsColliding(EntityPos pos, double impactSpeed)
	{
		pos.Motion.Set(0.0, 0.0, 0.0);
		if (beforeCollided || !(World is IServerWorldAccessor) || World.ElapsedMilliseconds <= msCollide + 500)
		{
			return;
		}
		if (impactSpeed >= 0.07)
		{
			World.PlaySoundAt(new AssetLocation("sounds/arrow-impact"), this, null, randomizePitch: false);
			WatchedAttributes.MarkAllDirty();
			if (DamageStackOnImpact)
			{
				ProjectileStack.Collectible.DamageItem(World, this, new DummySlot(ProjectileStack));
				if (((ProjectileStack == null || ProjectileStack.Collectible.GetRemainingDurability(ProjectileStack) != 0) ? 1 : 0) <= (false ? 1 : 0))
				{
					Die();
				}
			}
		}
		TryAttackEntity(impactSpeed);
		msCollide = World.ElapsedMilliseconds;
		beforeCollided = true;
	}

	protected virtual bool TryAttackEntity(double impactSpeed)
	{
		if (World is IClientWorldAccessor || World.ElapsedMilliseconds <= msCollide + 250)
		{
			return false;
		}
		if (impactSpeed <= 0.01)
		{
			return false;
		}
		_ = base.SidedPos;
		Cuboidd projectileBox = SelectionBox.ToDouble().Translate(ServerPos.X, ServerPos.Y, ServerPos.Z);
		if (ServerPos.Motion.X < 0.0)
		{
			projectileBox.X1 += 1.5 * ServerPos.Motion.X;
		}
		else
		{
			projectileBox.X2 += 1.5 * ServerPos.Motion.X;
		}
		if (ServerPos.Motion.Y < 0.0)
		{
			projectileBox.Y1 += 1.5 * ServerPos.Motion.Y;
		}
		else
		{
			projectileBox.Y2 += 1.5 * ServerPos.Motion.Y;
		}
		if (ServerPos.Motion.Z < 0.0)
		{
			projectileBox.Z1 += 1.5 * ServerPos.Motion.Z;
		}
		else
		{
			projectileBox.Z2 += 1.5 * ServerPos.Motion.Z;
		}
		Entity entity = World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, delegate(Entity e)
		{
			if (e.EntityId == EntityId || !e.IsInteractable)
			{
				return false;
			}
			if (entitiesHit.Contains(e.EntityId))
			{
				return false;
			}
			if (FiredBy != null && e.EntityId == FiredBy.EntityId && World.ElapsedMilliseconds - msLaunch < 500)
			{
				return false;
			}
			return (e.EntityId != FiredByMountEntityId || World.ElapsedMilliseconds - msLaunch >= 500) && e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).IntersectsOrTouches(projectileBox);
		});
		if (entity != null)
		{
			entitiesHit.Add(entity.EntityId);
			impactOnEntity(entity);
			return true;
		}
		return false;
	}

	protected virtual void impactOnEntity(Entity entity)
	{
		if (!Alive)
		{
			return;
		}
		EntityHit = true;
		EntityPos pos = base.SidedPos;
		IServerPlayer fromPlayer = null;
		if (FiredBy is EntityPlayer)
		{
			fromPlayer = (FiredBy as EntityPlayer).Player as IServerPlayer;
		}
		bool targetIsPlayer = entity is EntityPlayer;
		bool targetIsCreature = entity is EntityAgent;
		bool canDamage = true;
		ICoreServerAPI sapi = World.Api as ICoreServerAPI;
		if (fromPlayer != null)
		{
			if (targetIsPlayer && (!sapi.Server.Config.AllowPvP || !fromPlayer.HasPrivilege("attackplayers")))
			{
				canDamage = false;
			}
			if (targetIsCreature && !fromPlayer.HasPrivilege("attackcreatures"))
			{
				canDamage = false;
			}
		}
		msCollide = World.ElapsedMilliseconds;
		if (canDamage && World.Side == EnumAppSide.Server)
		{
			World.PlaySoundAt(new AssetLocation("sounds/arrow-impact"), this, null, randomizePitch: false, 24f);
			float dmg = Damage;
			if (FiredBy != null)
			{
				dmg *= FiredBy.Stats.GetBlended("rangedWeaponsDamage");
			}
			bool didDamage = entity.ReceiveDamage(new DamageSource
			{
				Source = ((fromPlayer != null) ? EnumDamageSource.Player : EnumDamageSource.Entity),
				SourceEntity = this,
				CauseEntity = FiredBy,
				Type = DamageType,
				DamageTier = DamageTier
			}, dmg);
			float kbresist = entity.Properties.KnockbackResistance;
			entity.SidedPos.Motion.Add((double)kbresist * pos.Motion.X * (double)Weight, (double)kbresist * pos.Motion.Y * (double)Weight, (double)kbresist * pos.Motion.Z * (double)Weight);
			int leftDurability = 1;
			if (DamageStackOnImpact)
			{
				ProjectileStack.Collectible.DamageItem(entity.World, entity, new DummySlot(ProjectileStack));
				leftDurability = ((ProjectileStack == null) ? 1 : ProjectileStack.Collectible.GetRemainingDurability(ProjectileStack));
			}
			if (!(World.Rand.NextDouble() < (double)DropOnImpactChance) || leftDurability <= 0)
			{
				Die();
			}
			if (FiredBy is EntityPlayer && didDamage)
			{
				World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (FiredBy as EntityPlayer).Player, randomizePitch: false, 24f);
			}
		}
		pos.Motion.Set(0.0, 0.0, 0.0);
	}

	public virtual void SetInitialRotation()
	{
		EntityPos pos = ServerPos;
		double speed = pos.Motion.Length();
		if (speed > 0.01)
		{
			pos.Pitch = 0f;
			pos.Yaw = (float)Math.PI + (float)Math.Atan2(pos.Motion.X / speed, pos.Motion.Z / speed);
			pos.Roll = 0f - (float)Math.Asin(GameMath.Clamp((0.0 - pos.Motion.Y) / speed, -1.0, 1.0));
		}
	}

	public virtual void SetRotation()
	{
		EntityPos pos = ((World is IServerWorldAccessor) ? ServerPos : Pos);
		double speed = pos.Motion.Length();
		if (speed > 0.01)
		{
			pos.Pitch = 0f;
			pos.Yaw = (float)Math.PI + (float)Math.Atan2(pos.Motion.X / speed, pos.Motion.Z / speed) + GameMath.Cos((float)(World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
			pos.Roll = 0f - (float)Math.Asin(GameMath.Clamp((0.0 - pos.Motion.Y) / speed, -1.0, 1.0)) + GameMath.Sin((float)(World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
		}
	}

	public override bool CanCollect(Entity byEntity)
	{
		if (!NonCollectible && Alive && World.ElapsedMilliseconds - msLaunch > 1000)
		{
			return ServerPos.Motion.Length() < 0.01;
		}
		return false;
	}

	public override ItemStack OnCollected(Entity byEntity)
	{
		ProjectileStack.ResolveBlockOrItem(World);
		return ProjectileStack;
	}

	public override void OnCollideWithLiquid()
	{
		base.OnCollideWithLiquid();
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		base.ToBytes(writer, forClient);
		writer.Write(beforeCollided);
		ProjectileStack.ToBytes(writer);
	}

	public override void FromBytes(BinaryReader reader, bool fromServer)
	{
		base.FromBytes(reader, fromServer);
		beforeCollided = reader.ReadBoolean();
		ProjectileStack = new ItemStack(reader);
	}
}
