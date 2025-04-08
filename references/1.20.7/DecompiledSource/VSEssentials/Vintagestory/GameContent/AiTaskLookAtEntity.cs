using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskLookAtEntity : AiTaskBaseTargetable
{
	public bool manualExecute;

	public float moveSpeed = 0.02f;

	public float seekingRange = 25f;

	public float maxFollowTime = 60f;

	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private float curTurnRadPerSec;

	private float maxTurnAngleRad = (float)Math.PI * 2f;

	private float spawnAngleRad;

	public AiTaskLookAtEntity(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		maxTurnAngleRad = taskConfig["maxTurnAngleDeg"].AsFloat(360f) * ((float)Math.PI / 180f);
		spawnAngleRad = entity.Attributes.GetFloat("spawnAngleRad");
	}

	public override bool ShouldExecute()
	{
		if (!manualExecute)
		{
			targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, seekingRange, (Entity e) => IsTargetableEntity(e, seekingRange), EnumEntitySearchType.Creatures);
			return targetEntity != null;
		}
		return false;
	}

	public float MinDistanceToTarget()
	{
		return Math.Max(0.8f, targetEntity.SelectionBox.XSize / 2f + entity.SelectionBox.XSize / 2f);
	}

	public override void StartExecute()
	{
		base.StartExecute();
		if (entity?.Properties.Server?.Attributes != null)
		{
			minTurnAnglePerSec = entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetFloat("minTurnAnglePerSec", 250f);
			maxTurnAnglePerSec = entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetFloat("maxTurnAnglePerSec", 450f);
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
		Vec3f targetVec = new Vec3f();
		targetVec.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));
		float desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);
		if (maxTurnAngleRad < (float)Math.PI)
		{
			desiredYaw = GameMath.Clamp(desiredYaw, spawnAngleRad - maxTurnAngleRad, spawnAngleRad + maxTurnAngleRad);
		}
		float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
		entity.ServerPos.Yaw += GameMath.Clamp(yawDist, (0f - curTurnRadPerSec) * dt, curTurnRadPerSec * dt);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		return (double)Math.Abs(yawDist) > 0.01;
	}

	public override bool Notify(string key, object data)
	{
		return false;
	}
}
