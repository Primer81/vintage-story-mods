using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityThrownBeenade : Entity
{
	private bool beforeCollided;

	private bool stuck;

	private long msLaunch;

	private Vec3d motionBeforeCollide = new Vec3d();

	public Entity FiredBy;

	public float Damage;

	public ItemStack ProjectileStack;

	public override bool IsInteractable => false;

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		msLaunch = World.ElapsedMilliseconds;
		if (ProjectileStack?.Collectible != null)
		{
			ProjectileStack.ResolveBlockOrItem(World);
		}
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
		pos.Pitch = (float)Math.PI / 2f;
		pos.Roll = 0f;
		pos.Yaw = (float)Math.PI / 2f;
		if (stuck)
		{
			if (!beforeCollided && World.Side == EnumAppSide.Server)
			{
				GameMath.Clamp((float)motionBeforeCollide.Length() * 4f, 0f, 1f);
				if (CollidedHorizontally || CollidedVertically)
				{
					OnImpact();
					return;
				}
				WatchedAttributes.MarkAllDirty();
			}
			beforeCollided = true;
			return;
		}
		if (Damage > 0f && World.Side == EnumAppSide.Server)
		{
			Entity entity = World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, (Entity e) => e.EntityId != EntityId && (FiredBy == null || e.EntityId != FiredBy.EntityId || World.ElapsedMilliseconds - msLaunch >= 500) && e.IsInteractable && e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).ShortestDistanceFrom(ServerPos.X, ServerPos.Y, ServerPos.Z) < 0.5);
			if (entity != null)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Entity,
					SourceEntity = this,
					CauseEntity = FiredBy,
					Type = EnumDamageType.BluntAttack
				}, Damage);
				OnImpact();
				return;
			}
		}
		beforeCollided = false;
		motionBeforeCollide.Set(pos.Motion.X, pos.Motion.Y, pos.Motion.Z);
	}

	public void OnImpact()
	{
		World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), this, null, randomizePitch: false);
		World.SpawnCubeParticles(base.SidedPos.XYZ.OffsetCopy(0.0, 0.2, 0.0), ProjectileStack, 0.8f, 20);
		Die();
		EntityProperties type = World.GetEntityType(new AssetLocation("beemob"));
		Entity entity = World.ClassRegistry.CreateEntity(type);
		if (entity != null)
		{
			entity.ServerPos.X = base.SidedPos.X + 0.5;
			entity.ServerPos.Y = base.SidedPos.Y + 0.5;
			entity.ServerPos.Z = base.SidedPos.Z + 0.5;
			entity.ServerPos.Yaw = (float)World.Rand.NextDouble() * 2f * (float)Math.PI;
			entity.Pos.SetFrom(entity.ServerPos);
			entity.Attributes.SetString("origin", "brokenbeenade");
			World.SpawnEntity(entity);
		}
	}

	public override bool CanCollect(Entity byEntity)
	{
		return false;
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
		ProjectileStack = ((World == null) ? new ItemStack(reader) : new ItemStack(reader, World));
	}
}
