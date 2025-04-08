using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModuleGravity : PModule
{
	private double gravityPerSecond = GlobalConstants.GravityPerSecond;

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			gravityPerSecond = GlobalConstants.GravityPerSecond * (float)config["gravityFactor"].AsDouble(1.0);
		}
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		if ((!controls.IsFlying || controls.Gliding) && entity.Properties.Habitat != EnumHabitat.Air && ((entity.Properties.Habitat != 0 && entity.Properties.Habitat != EnumHabitat.Underwater) || !entity.Swimming))
		{
			return !controls.IsClimbing;
		}
		return false;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if ((!entity.Swimming || !controls.TriesToMove || !entity.Alive) && entity.ApplyGravity && pos.Y > -100.0)
		{
			double gravity = (gravityPerSecond + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y)) * (double)(entity.FeetInLiquid ? 0.33f : 1f) * (double)dt;
			pos.Motion.Y -= gravity * GameMath.Clamp(1.0 - 50.0 * controls.GlideSpeed * controls.GlideSpeed, 0.0, 1.0);
		}
	}
}
