using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskTurretMode : AiTaskBaseTargetable
{
	private int durationMs;

	private int releaseAtMs;

	private long lastSearchTotalMs;

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

	private bool immobile;

	private float sensingRange;

	private float firingRangeMin;

	private float firingRangeMax;

	private float abortRange;

	private EnumTurretState currentState;

	private float currentStateTime;

	private bool executing;

	private EntityProjectile prevProjectile;

	private double overshootAdjustment;

	private bool inFiringRange
	{
		get
		{
			double range = targetEntity.ServerPos.DistanceTo(entity.ServerPos);
			if (range >= (double)firingRangeMin)
			{
				return range <= (double)firingRangeMax;
			}
			return false;
		}
	}

	private bool inSensingRange => targetEntity.ServerPos.DistanceTo(entity.ServerPos) <= (double)sensingRange;

	private bool inAbortRange => targetEntity.ServerPos.DistanceTo(entity.ServerPos) <= (double)abortRange;

	public AiTaskTurretMode(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		durationMs = taskConfig["durationMs"].AsInt(1500);
		releaseAtMs = taskConfig["releaseAtMs"].AsInt(1000);
		projectileDamage = taskConfig["projectileDamage"].AsFloat(1f);
		projectileDamageTier = taskConfig["projectileDamageTier"].AsInt(1);
		sensingRange = taskConfig["sensingRange"].AsFloat(30f);
		firingRangeMin = taskConfig["firingRangeMin"].AsFloat(14f);
		firingRangeMax = taskConfig["firingRangeMax"].AsFloat(26f);
		abortRange = taskConfig["abortRange"].AsFloat(14f);
		projectileCode = AssetLocation.Create(taskConfig["projectileCode"].AsString("thrownstone-{rock}"), entity.Code.Domain);
		immobile = taskConfig["immobile"].AsBool();
		maxTurnAngleRad = taskConfig["maxTurnAngleDeg"].AsFloat(360f) * ((float)Math.PI / 180f);
		maxOffAngleThrowRad = taskConfig["maxOffAngleThrowDeg"].AsFloat() * ((float)Math.PI / 180f);
		spawnAngleRad = entity.Attributes.GetFloat("spawnAngleRad");
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		entity.AnimManager.OnAnimationStopped += AnimManager_OnAnimationStopped;
	}

	private void AnimManager_OnAnimationStopped(string anim)
	{
		if (executing && targetEntity != null)
		{
			updateState();
		}
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
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		float range = sensingRange;
		targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, range, (Entity e) => IsTargetableEntity(e, range) && hasDirectContact(e, range, range / 2f) && aimableDirection(e), EnumEntitySearchType.Creatures);
		if (targetEntity != null)
		{
			return !inAbortRange;
		}
		return false;
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
		currentState = EnumTurretState.Idle;
		currentStateTime = 0f;
		executing = true;
	}

	private void updateState()
	{
		switch (currentState)
		{
		case EnumTurretState.Idle:
			if (inFiringRange)
			{
				entity.StartAnimation("load");
				currentState = EnumTurretState.TurretMode;
				currentStateTime = 0f;
			}
			else if (inSensingRange)
			{
				entity.StartAnimation("turret");
				currentState = EnumTurretState.TurretMode;
				currentStateTime = 0f;
			}
			break;
		case EnumTurretState.TurretMode:
			if (isAnimDone("turret"))
			{
				if (inAbortRange)
				{
					abort();
				}
				else if (inFiringRange)
				{
					currentState = EnumTurretState.TurretModeLoad;
					entity.StopAnimation("turret");
					entity.StartAnimation("load-fromturretpose");
					entity.World.PlaySoundAt("sounds/creature/bowtorn/draw", entity, null, randomizePitch: false);
					currentStateTime = 0f;
				}
				else if (currentStateTime > 5f)
				{
					currentState = EnumTurretState.Stop;
					entity.StopAnimation("turret");
				}
			}
			break;
		case EnumTurretState.TurretModeLoad:
			if (isAnimDone("load"))
			{
				entity.StartAnimation("hold");
				currentState = EnumTurretState.TurretModeHold;
				currentStateTime = 0f;
			}
			break;
		case EnumTurretState.TurretModeHold:
			if (inFiringRange || inAbortRange)
			{
				if ((double)currentStateTime > 1.25)
				{
					fireProjectile();
					currentState = EnumTurretState.TurretModeFired;
					entity.StopAnimation("hold");
					entity.StartAnimation("fire");
				}
			}
			else if (currentStateTime > 2f)
			{
				currentState = EnumTurretState.TurretModeUnload;
				entity.StopAnimation("hold");
				entity.StartAnimation("unload");
			}
			break;
		case EnumTurretState.TurretModeUnload:
			if (isAnimDone("unload"))
			{
				currentState = EnumTurretState.Stop;
			}
			break;
		case EnumTurretState.TurretModeFired:
		{
			float range = sensingRange;
			if (inAbortRange || !targetEntity.Alive || !targetablePlayerMode((targetEntity as EntityPlayer)?.Player) || !hasDirectContact(targetEntity, range, range / 2f))
			{
				abort();
			}
			else if (inSensingRange)
			{
				currentState = EnumTurretState.TurretModeReload;
				entity.StartAnimation("reload");
				entity.World.PlaySoundAt("sounds/creature/bowtorn/reload", entity, null, randomizePitch: false);
			}
			break;
		}
		case EnumTurretState.TurretModeReload:
			if (isAnimDone("reload"))
			{
				if (inAbortRange)
				{
					abort();
					break;
				}
				entity.World.PlaySoundAt("sounds/creature/bowtorn/draw", entity, null, randomizePitch: false);
				currentState = EnumTurretState.TurretModeLoad;
			}
			break;
		}
	}

	private void abort()
	{
		currentState = EnumTurretState.Stop;
		entity.StopAnimation("hold");
		entity.StopAnimation("turret");
		AiTaskManager taskManager = entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
		taskManager.GetTask<AiTaskStayInRange>().targetEntity = targetEntity;
		taskManager.ExecuteTask<AiTaskStayInRange>();
	}

	private bool isAnimDone(string anim)
	{
		RunningAnimation tstate = entity.AnimManager.GetAnimationState(anim);
		if (tstate.Running)
		{
			return (double)tstate.AnimProgress >= 0.95;
		}
		return true;
	}

	private void fireProjectile()
	{
		AssetLocation loc = projectileCode.Clone();
		if (projectileCode.Path.Contains('{'))
		{
			string rocktype = "granite";
			IMapChunk mc = entity.World.BlockAccessor.GetMapChunkAtBlockPos(entity.Pos.AsBlockPos);
			if (mc != null)
			{
				int lz = (int)entity.Pos.Z % 32;
				int lx = (int)entity.Pos.X % 32;
				rocktype = entity.World.Blocks[mc.TopRockIdMap[lz * 32 + lx]].Variant["rock"] ?? "granite";
			}
			loc.Path = loc.Path.Replace("{rock}", rocktype);
		}
		EntityProperties type = entity.World.GetEntityType(loc);
		if (type == null)
		{
			throw new Exception("No such projectile exists - " + loc);
		}
		EntityProjectile entitypr = entity.World.ClassRegistry.CreateEntity(type) as EntityProjectile;
		entitypr.FiredBy = entity;
		entitypr.Damage = projectileDamage;
		entitypr.DamageTier = projectileDamageTier;
		entitypr.ProjectileStack = new ItemStack(entity.World.GetItem(new AssetLocation("stone-granite")));
		entitypr.NonCollectible = true;
		Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0);
		Vec3d targetPos = targetEntity.ServerPos.XYZ.Add(0.0, targetEntity.LocalEyePos.Y, 0.0) + targetEntity.ServerPos.Motion * 8f;
		double dist = pos.DistanceTo(targetPos);
		double prevVelo = prevProjectile?.ServerPos.Motion.Length() ?? 0.0;
		if (prevProjectile != null && !prevProjectile.EntityHit && prevVelo < 0.01)
		{
			float impactDistance = pos.DistanceTo(prevProjectile.ServerPos.XYZ);
			if (dist > (double)impactDistance)
			{
				overshootAdjustment = (0.0 - ((double)impactDistance - dist)) / 4.0;
			}
			else
			{
				overshootAdjustment = (dist - (double)impactDistance) / 4.0;
			}
		}
		dist += overshootAdjustment;
		double distf = Math.Pow(dist, 0.2);
		Vec3d velocity = (targetPos - pos).Normalize() * GameMath.Clamp(distf - 1.0, 0.10000000149011612, 1.0);
		velocity.Y += (dist - 10.0) / 200.0;
		entitypr.ServerPos.SetPosWithDimension(entity.ServerPos.XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0));
		entitypr.ServerPos.Motion.Set(velocity);
		entitypr.SetInitialRotation();
		entitypr.Pos.SetFrom(entitypr.ServerPos);
		entitypr.World = entity.World;
		entity.World.SpawnEntity(entitypr);
		if (prevProjectile == null || prevVelo < 0.01)
		{
			prevProjectile = entitypr;
		}
		entity.World.PlaySoundAt("sounds/creature/bowtorn/release", entity, null, randomizePitch: false);
	}

	public override bool ContinueExecute(float dt)
	{
		currentStateTime += dt;
		updateState();
		float desiredYaw = getAimYaw(targetEntity);
		desiredYaw = GameMath.Clamp(desiredYaw, spawnAngleRad - maxTurnAngleRad, spawnAngleRad + maxTurnAngleRad);
		float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
		entity.ServerPos.Yaw += GameMath.Clamp(yawDist, (0f - curTurnRadPerSec) * dt, curTurnRadPerSec * dt);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		return currentState != EnumTurretState.Stop;
	}

	private float getAimYaw(Entity targetEntity)
	{
		Vec3f targetVec = new Vec3f();
		targetVec.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));
		return (float)Math.Atan2(targetVec.X, targetVec.Z);
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		entity.StopAnimation("turret");
		entity.StopAnimation("hold");
		executing = false;
		prevProjectile = null;
	}
}
