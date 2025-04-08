using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModulePlayerInAir : PModuleInAir
{
	private float airMovingStrengthFalling;

	public override void Initialize(JsonObject config, Entity entity)
	{
		base.Initialize(config, entity);
		airMovingStrengthFalling = AirMovingStrength / 4f;
	}

	public override void ApplyFreeFall(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.IsClimbing)
		{
			base.ApplyFreeFall(dt, entity, pos, controls);
			return;
		}
		float strength = AirMovingStrength * Math.Min(1f, ((EntityPlayer)entity).walkSpeed) * dt * 60f;
		if (!controls.Jump)
		{
			strength = airMovingStrengthFalling;
			pos.Motion.X *= (float)Math.Pow(0.9800000190734863, dt * 33f);
			pos.Motion.Z *= (float)Math.Pow(0.9800000190734863, dt * 33f);
		}
		pos.Motion.Add(controls.WalkVector.X * (double)strength, controls.WalkVector.Y * (double)strength, controls.WalkVector.Z * (double)strength);
	}

	public override void ApplyFlying(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if (controls.Gliding)
		{
			double cosPitch = Math.Cos(pos.Pitch);
			double num = Math.Sin(pos.Pitch);
			double cosYaw = Math.Cos(pos.Yaw);
			double sinYaw = Math.Sin(pos.Yaw);
			double glideFactor = num + 0.15;
			controls.GlideSpeed = GameMath.Clamp(controls.GlideSpeed - glideFactor * (double)dt * 0.25, 0.004999999888241291, 0.75);
			double glideSpeed = GameMath.Clamp(max: (double)entity.Stats.GetBlended("gliderSpeedMax") - 0.8, val: controls.GlideSpeed, min: 0.004999999888241291);
			float gliderLiftMax = entity.Stats.GetBlended("gliderLiftMax");
			double pitch = Math.Min(num * glideSpeed, gliderLiftMax);
			pos.Motion.Add((0.0 - cosPitch) * sinYaw * glideSpeed, pitch, (0.0 - cosPitch) * cosYaw * glideSpeed);
			pos.Motion.Mul(GameMath.Clamp(1.0 - pos.Motion.Length() * 0.12999999523162842, 0.0, 1.0));
		}
		else
		{
			base.ApplyFlying(dt, entity, pos, controls);
		}
	}
}
