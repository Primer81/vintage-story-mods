using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public class PModuleInAir : PModule
{
	public float AirMovingStrength = 0.05f;

	public double WallDragFactor = 0.30000001192092896;

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			WallDragFactor = 0.3 * (double)(float)config["wallDragFactor"].AsDouble(1.0);
			AirMovingStrength = (float)config["airMovingStrength"].AsDouble(0.05);
		}
	}

	/// <summary>
	/// Applicable if the player is in fly mode or the entity isn't colliding with anything including liquid.
	/// Must be alive.
	/// </summary>
	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsFlying || (!entity.Collided && !entity.FeetInLiquid))
		{
			return entity.Alive;
		}
		return false;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsFlying)
		{
			ApplyFlying(dt, entity, pos, controls);
		}
		else
		{
			ApplyFreeFall(dt, entity, pos, controls);
		}
	}

	public virtual void ApplyFreeFall(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsClimbing)
		{
			pos.Motion.Add(controls.WalkVector);
			pos.Motion.X *= Math.Pow(1.0 - WallDragFactor, dt * 60f);
			pos.Motion.Y *= Math.Pow(1.0 - WallDragFactor, dt * 60f);
			pos.Motion.Z *= Math.Pow(1.0 - WallDragFactor, dt * 60f);
		}
		else
		{
			float strength = AirMovingStrength * dt * 60f;
			pos.Motion.Add(controls.WalkVector.X * (double)strength, controls.WalkVector.Y * (double)strength, controls.WalkVector.Z * (double)strength);
		}
	}

	/// <summary>
	/// Creative flight movement, possibly glider too?
	/// </summary>
	public virtual void ApplyFlying(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		double deltaY = controls.FlyVector.Y;
		if (controls.Up || controls.Down)
		{
			float moveSpeed = Math.Min(0.2f, dt) * GlobalConstants.BaseMoveSpeed * controls.MovespeedMultiplier / 2f;
			deltaY = (controls.Up ? moveSpeed : 0f) + (controls.Down ? (0f - moveSpeed) : 0f);
		}
		if (deltaY > 0.0 && pos.Y % 32768.0 > 24576.0)
		{
			deltaY = 0.0;
		}
		pos.Motion.Add(controls.FlyVector.X, deltaY, controls.FlyVector.Z);
	}
}
