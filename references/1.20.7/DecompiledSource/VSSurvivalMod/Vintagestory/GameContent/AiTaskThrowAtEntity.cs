using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskThrowAtEntity : AiTaskBaseTargetable
{
	private int durationMs;

	private int releaseAtMs;

	private long lastSearchTotalMs;

	private float maxDist = 15f;

	protected int searchWaitMs = 2000;

	private float accum;

	private bool didThrow;

	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private float curTurnRadPerSec;

	private float projectileDamage;

	private int projectileDamageTier;

	private AssetLocation projectileCode;

	private float maxTurnAngleRad;

	private float maxOffAngleThrowRad;

	private float spawnAngleRad;

	private float yawInaccuracy;

	private bool immobile;

	public AiTaskThrowAtEntity(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		durationMs = taskConfig["durationMs"].AsInt(1500);
		releaseAtMs = taskConfig["releaseAtMs"].AsInt(1000);
		projectileDamage = taskConfig["projectileDamage"].AsFloat(1f);
		projectileDamageTier = taskConfig["projectileDamageTier"].AsInt();
		maxDist = taskConfig["maxDist"].AsFloat(15f);
		yawInaccuracy = taskConfig["yawInaccuracy"].AsFloat();
		projectileCode = AssetLocation.Create(taskConfig["projectileCode"].AsString("thrownstone-{rock}"), entity.Code.Domain);
		immobile = taskConfig["immobile"].AsBool();
		maxTurnAngleRad = taskConfig["maxTurnAngleDeg"].AsFloat(360f) * ((float)Math.PI / 180f);
		maxOffAngleThrowRad = taskConfig["maxOffAngleThrowDeg"].AsFloat() * ((float)Math.PI / 180f);
		spawnAngleRad = entity.Attributes.GetFloat("spawnAngleRad");
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > 0.10000000149011612 && (whenInEmotionState == null || !IsInEmotionState(whenInEmotionState)))
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (whenInEmotionState == null && base.rand.NextDouble() > 0.5)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		float range = maxDist;
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, range, (Entity e) => IsTargetableEntity(e, range) && hasDirectContact(e, range, range / 2f) && aimableDirection(e), EnumEntitySearchType.Creatures);
		return targetEntity != null;
	}

	private bool aimableDirection(Entity e)
	{
		if (!immobile)
		{
			return true;
		}
		float aimYaw = getAimYaw(e);
		if (aimYaw > spawnAngleRad - maxTurnAngleRad - maxOffAngleThrowRad)
		{
			return aimYaw < spawnAngleRad + maxTurnAngleRad + maxOffAngleThrowRad;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		accum = 0f;
		didThrow = false;
		ITreeAttribute pathfinder = entity?.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder");
		if (pathfinder != null)
		{
			minTurnAnglePerSec = pathfinder.GetFloat("minTurnAnglePerSec", 250f);
			maxTurnAnglePerSec = pathfinder.GetFloat("maxTurnAnglePerSec", 450f);
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
		curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= (float)Math.PI / 180f;
	}

	public override bool ContinueExecute(float dt)
	{
		float desiredYaw = getAimYaw(targetEntity);
		desiredYaw = GameMath.Clamp(desiredYaw, spawnAngleRad - maxTurnAngleRad, spawnAngleRad + maxTurnAngleRad);
		float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
		entity.ServerPos.Yaw += GameMath.Clamp(yawDist, (0f - curTurnRadPerSec) * dt, curTurnRadPerSec * dt);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		if (animMeta != null)
		{
			animMeta.EaseInSpeed = 1f;
			animMeta.EaseOutSpeed = 1f;
			entity.AnimManager.StartAnimation(animMeta);
		}
		accum += dt;
		if (accum > (float)releaseAtMs / 1000f && !didThrow)
		{
			didThrow = true;
			AssetLocation loc = projectileCode.Clone();
			string rocktype = "granite";
			IMapChunk mc = entity.World.BlockAccessor.GetMapChunkAtBlockPos(entity.Pos.AsBlockPos);
			if (mc != null)
			{
				int lz = (int)entity.Pos.Z % 32;
				int lx = (int)entity.Pos.X % 32;
				rocktype = entity.World.Blocks[mc.TopRockIdMap[lz * 32 + lx]].Variant["rock"] ?? "granite";
			}
			loc.Path = loc.Path.Replace("{rock}", rocktype);
			EntityProperties type = entity.World.GetEntityType(loc);
			if (type == null)
			{
				throw new Exception("No such projectile exists - " + loc);
			}
			EntityThrownStone entitypr = entity.World.ClassRegistry.CreateEntity(type) as EntityThrownStone;
			entitypr.FiredBy = entity;
			entitypr.Damage = projectileDamage;
			entitypr.DamageTier = projectileDamageTier;
			entitypr.ProjectileStack = new ItemStack(entity.World.GetItem(new AssetLocation("stone-granite")));
			entitypr.NonCollectible = true;
			Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0);
			Vec3d targetPos = targetEntity.ServerPos.XYZ.Add(0.0, targetEntity.LocalEyePos.Y, 0.0) + targetEntity.ServerPos.Motion * 8f;
			double distf = Math.Pow(pos.SquareDistanceTo(targetPos), 0.1);
			Vec3d velocity = (targetPos - pos).Normalize() * GameMath.Clamp(distf - 1.0, 0.10000000149011612, 1.0);
			if (yawInaccuracy > 0f)
			{
				Random rnd = entity.World.Rand;
				velocity = velocity.RotatedCopy((float)(rnd.NextDouble() * (double)yawInaccuracy - (double)yawInaccuracy / 2.0));
			}
			entitypr.ServerPos.SetPosWithDimension(entity.ServerPos.BehindCopy(0.21).XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0));
			entitypr.ServerPos.Motion.Set(velocity);
			entitypr.Pos.SetFrom(entitypr.ServerPos);
			entitypr.World = entity.World;
			entity.World.SpawnEntity(entitypr);
		}
		return accum < (float)durationMs / 1000f;
	}

	private float getAimYaw(Entity targetEntity)
	{
		Vec3f targetVec = new Vec3f();
		targetVec.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));
		return (float)Math.Atan2(targetVec.X, targetVec.Z);
	}
}
