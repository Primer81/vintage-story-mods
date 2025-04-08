using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskFleeEntity : AiTaskBaseTargetable
{
	private Vec3d targetPos = new Vec3d();

	private float targetYaw;

	private float moveSpeed = 0.02f;

	private float seekingRange = 25f;

	private float executionChance = 0.1f;

	private float fleeingDistance = 31f;

	private float minDayLight = -1f;

	private float fleeDurationMs = 5000f;

	private float instafleeOnDamageChance;

	private bool cancelOnHurt;

	private long fleeStartMs;

	private bool stuck;

	private bool lowStabilityAttracted;

	private bool ignoreDeepDayLight;

	private bool cancelNow;

	private float nowFleeingDistance;

	private bool instafleenow;

	private readonly Vec3d ownPos = new Vec3d();

	private Vec3d tmpTargetPos = new Vec3d();

	public override bool AggressiveTargeting => false;

	public AiTaskFleeEntity(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
		seekingRange = taskConfig["seekingRange"].AsFloat(25f);
		executionChance = taskConfig["executionChance"].AsFloat(0.1f);
		minDayLight = taskConfig["minDayLight"].AsFloat(-1f);
		cancelOnHurt = taskConfig["cancelOnHurt"].AsBool();
		ignoreDeepDayLight = taskConfig["ignoreDeepDayLight"].AsBool();
		fleeingDistance = taskConfig["fleeingDistance"].AsFloat(seekingRange + 15f);
		fleeDurationMs = taskConfig["fleeDurationMs"].AsInt(9000);
		instafleeOnDamageChance = taskConfig["instafleeOnDamageChance"].AsFloat();
		lowStabilityAttracted = entity.World.Config.GetString("temporalStability").ToBool(defaultValue: true) && (entity.Properties.Attributes?["spawnCloserDuringLowStability"].AsBool() ?? false);
	}

	public override bool ShouldExecute()
	{
		soundChance = Math.Min(1.01f, soundChance + 0.002f);
		if (instafleenow)
		{
			return TryInstaFlee();
		}
		if (base.rand.NextDouble() > (double)executionChance)
		{
			return false;
		}
		if (base.noEntityCodes && (attackedByEntity == null || !retaliateAttacks))
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (minDayLight > 0f)
		{
			if (entity.Attributes.GetBool("ignoreDaylightFlee"))
			{
				return false;
			}
			if (ignoreDeepDayLight && entity.ServerPos.Y < (double)(world.SeaLevel - 2))
			{
				return false;
			}
			if ((float)entity.World.BlockAccessor.GetLightLevel((int)entity.ServerPos.X, (int)entity.ServerPos.Y, (int)entity.ServerPos.Z, EnumLightLevelType.TimeOfDaySunLight) / (float)entity.World.SunBrightness < minDayLight)
			{
				return false;
			}
		}
		int generation = GetOwnGeneration();
		float fearReductionFactor = ((whenInEmotionState != null) ? 1f : Math.Max(0f, (tamingGenerations - (float)generation) / tamingGenerations));
		ownPos.SetWithDimension(entity.ServerPos);
		float hereRange = fearReductionFactor * seekingRange;
		entity.World.FrameProfiler.Mark("task-fleeentity-shouldexecute-init");
		if (lowStabilityAttracted)
		{
			targetEntity = partitionUtil.GetNearestEntity(ownPos, hereRange, delegate(Entity entity)
			{
				if (!(entity is EntityAgent))
				{
					return false;
				}
				if (!IsTargetableEntity(entity, hereRange))
				{
					return false;
				}
				return !(entity is EntityPlayer) || entity.WatchedAttributes.GetDouble("temporalStability", 1.0) > 0.25;
			}, EnumEntitySearchType.Creatures) as EntityAgent;
		}
		else
		{
			targetEntity = partitionUtil.GetNearestEntity(ownPos, hereRange, (Entity entity) => IsTargetableEntity(entity, hereRange) && entity is EntityAgent, EnumEntitySearchType.Creatures) as EntityAgent;
		}
		nowFleeingDistance = fleeingDistance;
		entity.World.FrameProfiler.Mark("task-fleeentity-shouldexecute-entitysearch");
		if (targetEntity != null)
		{
			float yaw = (float)Math.Atan2(targetEntity.ServerPos.X - entity.ServerPos.X, targetEntity.ServerPos.Z - entity.ServerPos.Z);
			updateTargetPosFleeMode(targetPos, yaw);
			return true;
		}
		return false;
	}

	private bool TryInstaFlee()
	{
		if (targetEntity == null || entity.ServerPos.DistanceTo(targetEntity.ServerPos) > (double)seekingRange)
		{
			float cosYaw = GameMath.Cos(entity.ServerPos.Yaw);
			float sinYaw = GameMath.Sin(entity.ServerPos.Yaw);
			double offset = 200.0;
			targetPos = new Vec3d(entity.ServerPos.X + (double)sinYaw * offset, entity.ServerPos.Y, entity.ServerPos.Z + (double)cosYaw * offset);
			targetYaw = entity.ServerPos.Yaw;
			targetEntity = null;
		}
		else
		{
			nowFleeingDistance = (float)entity.ServerPos.DistanceTo(targetEntity.ServerPos) + 15f;
			updateTargetPosFleeMode(targetPos, entity.ServerPos.Yaw);
		}
		instafleenow = false;
		return true;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		cancelNow = false;
		soundChance = Math.Max(0.025f, soundChance - 0.2f);
		float size = targetEntity?.SelectionBox.XSize ?? 0f;
		pathTraverser.WalkTowards(targetPos, moveSpeed, size + 0.2f, OnGoalReached, OnStuck);
		fleeStartMs = entity.World.ElapsedMilliseconds;
		stuck = false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (world.Rand.NextDouble() < 0.2)
		{
			float yaw = ((targetEntity == null) ? (0f - targetYaw) : ((float)Math.Atan2(targetEntity.ServerPos.X - entity.ServerPos.X, targetEntity.ServerPos.Z - entity.ServerPos.Z)));
			updateTargetPosFleeMode(tmpTargetPos.Set(targetPos), yaw);
			pathTraverser.CurrentTarget.X = tmpTargetPos.X;
			pathTraverser.CurrentTarget.Y = tmpTargetPos.Y;
			pathTraverser.CurrentTarget.Z = tmpTargetPos.Z;
			pathTraverser.Retarget();
		}
		if (targetEntity != null && entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos) > nowFleeingDistance * nowFleeingDistance)
		{
			return false;
		}
		if (targetEntity == null && entity.World.ElapsedMilliseconds - fleeStartMs > 5000)
		{
			return false;
		}
		if (world.Rand.NextDouble() < 0.25)
		{
			float sunlight = (float)entity.World.BlockAccessor.GetLightLevel((int)entity.ServerPos.X, (int)entity.ServerPos.Y, (int)entity.ServerPos.Z, EnumLightLevelType.TimeOfDaySunLight) / (float)entity.World.SunBrightness;
			if (((ignoreDeepDayLight && entity.ServerPos.Y < (double)(world.SeaLevel - 2)) || sunlight < minDayLight) && !entity.Attributes.GetBool("ignoreDaylightFlee"))
			{
				return false;
			}
		}
		if (!stuck && (targetEntity == null || targetEntity.Alive) && (float)(entity.World.ElapsedMilliseconds - fleeStartMs) < fleeDurationMs && !cancelNow)
		{
			return pathTraverser.Active;
		}
		return false;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		base.OnEntityHurt(source, damage);
		if (cancelOnHurt)
		{
			cancelNow = true;
		}
		if (source.Type != EnumDamageType.Heal && entity.World.Rand.NextDouble() < (double)instafleeOnDamageChance)
		{
			instafleenow = true;
			targetEntity = source.CauseEntity;
		}
	}

	public void InstaFleeFrom(Entity fromEntity)
	{
		instafleenow = true;
		targetEntity = fromEntity;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}

	private void OnStuck()
	{
		stuck = true;
	}

	private void OnGoalReached()
	{
		pathTraverser.Retarget();
	}

	public override bool CanSense(Entity e, double range)
	{
		if (e.EntityId == entity.EntityId)
		{
			return false;
		}
		if (e is EntityPlayer eplr)
		{
			return CanSensePlayer(eplr, range);
		}
		if (skipEntityCodes != null)
		{
			for (int i = 0; i < skipEntityCodes.Length; i++)
			{
				if (WildcardUtil.Match(skipEntityCodes[i], e.Code))
				{
					return false;
				}
			}
		}
		return true;
	}
}
