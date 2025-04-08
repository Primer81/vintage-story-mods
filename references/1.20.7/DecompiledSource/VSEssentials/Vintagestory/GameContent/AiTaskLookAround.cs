using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskLookAround : AiTaskBase
{
	public int minduration;

	public int maxduration;

	public float turnSpeedMul = 0.75f;

	public long idleUntilMs;

	public AiTaskLookAround(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		minduration = (taskConfig["minduration"]?.AsInt(2000)).Value;
		maxduration = (taskConfig["maxduration"]?.AsInt(4000)).Value;
		turnSpeedMul = (taskConfig["turnSpeedMul"]?.AsFloat(0.75f)).Value;
		idleUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
		base.LoadConfig(taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		return cooldownUntilMs < entity.World.ElapsedMilliseconds;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		idleUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
		entity.ServerPos.Yaw = (float)GameMath.Clamp(entity.World.Rand.NextDouble() * 6.2831854820251465, entity.ServerPos.Yaw - (float)Math.PI / 4f * GlobalConstants.OverallSpeedMultiplier * turnSpeedMul, entity.ServerPos.Yaw + (float)Math.PI / 4f * GlobalConstants.OverallSpeedMultiplier * turnSpeedMul);
	}

	public override bool ContinueExecute(float dt)
	{
		return entity.World.ElapsedMilliseconds < idleUntilMs;
	}
}
