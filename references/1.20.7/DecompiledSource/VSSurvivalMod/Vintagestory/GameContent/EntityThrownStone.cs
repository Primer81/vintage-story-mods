using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityThrownStone : Entity
{
	protected bool beforeCollided;

	protected bool stuck;

	protected long msLaunch;

	protected Vec3d motionBeforeCollide = new Vec3d();

	protected CollisionTester collTester = new CollisionTester();

	public Entity FiredBy;

	public float Damage;

	public int DamageTier;

	public ItemStack ProjectileStack;

	public float collidedAccum;

	public float VerticalImpactBreakChance;

	public float HorizontalImpactBreakChance = 0.8f;

	public float ImpactParticleSize = 1f;

	public int ImpactParticleCount = 20;

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
		if (ProjectileStack?.Collectible != null)
		{
			ProjectileStack.ResolveBlockOrItem(World);
		}
		GetBehavior<EntityBehaviorPassivePhysics>().CollisionYExtra = 0f;
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (ShouldDespawn)
		{
			return;
		}
		EntityPos pos = base.SidedPos;
		stuck = base.Collided;
		if (stuck)
		{
			pos.Pitch = 0f;
			pos.Roll = 0f;
			pos.Yaw = (float)Math.PI / 2f;
			collidedAccum += dt;
			if (NonCollectible && collidedAccum > 1f)
			{
				Die();
			}
		}
		else
		{
			pos.Pitch = (float)World.ElapsedMilliseconds / 300f % ((float)Math.PI * 2f);
			pos.Roll = 0f;
			pos.Yaw = (float)World.ElapsedMilliseconds / 400f % ((float)Math.PI * 2f);
		}
		if (World is IServerWorldAccessor)
		{
			Entity entity = World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, (Entity e) => e.EntityId != EntityId && (FiredBy == null || e.EntityId != FiredBy.EntityId || World.ElapsedMilliseconds - msLaunch >= 500) && e.IsInteractable && e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).ShortestDistanceFrom(ServerPos.X, ServerPos.Y, ServerPos.Z) < 0.5);
			if (entity != null)
			{
				bool didDamage = entity.ReceiveDamage(new DamageSource
				{
					Source = ((FiredBy is EntityPlayer) ? EnumDamageSource.Player : EnumDamageSource.Entity),
					SourceEntity = this,
					CauseEntity = FiredBy,
					Type = EnumDamageType.BluntAttack,
					DamageTier = DamageTier,
					YDirKnockbackDiv = 3f
				}, Damage);
				World.PlaySoundAt(new AssetLocation("sounds/thud"), this, null, randomizePitch: false);
				World.SpawnCubeParticles(entity.SidedPos.XYZ.OffsetCopy(0.0, 0.2, 0.0), ProjectileStack, 0.2f, ImpactParticleCount, ImpactParticleSize);
				if (FiredBy is EntityPlayer && didDamage)
				{
					World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (FiredBy as EntityPlayer).Player, randomizePitch: false, 24f);
				}
				Die();
				return;
			}
		}
		beforeCollided = false;
		motionBeforeCollide.Set(pos.Motion.X, pos.Motion.Y, pos.Motion.Z);
	}

	public override void OnCollided()
	{
		EntityPos pos = base.SidedPos;
		if (!beforeCollided && World is IServerWorldAccessor)
		{
			float strength = GameMath.Clamp((float)motionBeforeCollide.Length() * 4f, 0f, 1f);
			if (CollidedHorizontally)
			{
				float xdir = ((pos.Motion.X != 0.0) ? 1 : (-1));
				float zdir = ((pos.Motion.Z != 0.0) ? 1 : (-1));
				pos.Motion.X = (double)xdir * motionBeforeCollide.X * 0.4000000059604645;
				pos.Motion.Z = (double)zdir * motionBeforeCollide.Z * 0.4000000059604645;
				if (strength > 0.1f && World.Rand.NextDouble() > (double)(1f - HorizontalImpactBreakChance))
				{
					World.SpawnCubeParticles(base.SidedPos.XYZ.OffsetCopy(0.0, 0.2, 0.0), ProjectileStack, 0.5f, ImpactParticleCount, ImpactParticleSize, null, new Vec3f(xdir * (float)motionBeforeCollide.X * 8f, 0f, zdir * (float)motionBeforeCollide.Z * 8f));
					Die();
				}
			}
			if (CollidedVertically && motionBeforeCollide.Y <= 0.0)
			{
				pos.Motion.Y = GameMath.Clamp(motionBeforeCollide.Y * -0.30000001192092896, -0.10000000149011612, 0.10000000149011612);
				if (strength > 0.1f && World.Rand.NextDouble() > (double)(1f - VerticalImpactBreakChance))
				{
					World.SpawnCubeParticles(base.SidedPos.XYZ.OffsetCopy(0.0, 0.25, 0.0), ProjectileStack, 0.5f, ImpactParticleCount, ImpactParticleSize, null, new Vec3f((float)motionBeforeCollide.X * 8f, (float)(0.0 - motionBeforeCollide.Y) * 6f, (float)motionBeforeCollide.Z * 8f));
					Die();
				}
			}
			World.PlaySoundAt(new AssetLocation("sounds/thud"), this, null, randomizePitch: false, 32f, strength);
			WatchedAttributes.MarkAllDirty();
		}
		beforeCollided = true;
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
		if (motionBeforeCollide.Y <= 0.0)
		{
			base.SidedPos.Motion.Y = GameMath.Clamp(motionBeforeCollide.Y * -0.5, -0.10000000149011612, 0.10000000149011612);
			PositionBeforeFalling.Y = Pos.Y + 1.0;
		}
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
		ProjectileStack = ((World == null) ? new ItemStack(reader) : new ItemStack(reader, World));
	}
}
