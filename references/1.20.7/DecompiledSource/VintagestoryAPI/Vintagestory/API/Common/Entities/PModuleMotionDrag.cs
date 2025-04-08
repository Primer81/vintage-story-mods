using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public class PModuleMotionDrag : PModule
{
	private double waterDragValue = GlobalConstants.WaterDrag;

	private double airDragValue = GlobalConstants.AirDragAlways;

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			waterDragValue = 1f - (1f - GlobalConstants.WaterDrag) * (float)config["waterDragFactor"].AsDouble(1.0);
			airDragValue = 1f - (1f - GlobalConstants.AirDragAlways) * (float)config["airDragFallingFactor"].AsDouble(1.0);
		}
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		return true;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (entity.FeetInLiquid || entity.Swimming)
		{
			pos.Motion *= (float)Math.Pow(waterDragValue, dt * 33f);
		}
		else
		{
			pos.Motion *= (float)Math.Pow(airDragValue, dt * 33f);
		}
		if (controls.IsFlying && !controls.Gliding)
		{
			pos.Motion *= (float)Math.Pow(GlobalConstants.AirDragFlying, dt * 33f);
		}
	}
}
