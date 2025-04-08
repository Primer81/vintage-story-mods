using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class PModuleOnGround : PModule
{
	private long lastJump;

	private double groundDragFactor = 0.30000001192092896;

	private float accum;

	private float coyoteTimer;

	private float antiCoyoteTimer;

	private readonly Vec3d motionDelta = new Vec3d();

	public override void Initialize(JsonObject config, Entity entity)
	{
		if (config != null)
		{
			groundDragFactor = 0.3 * (double)(float)config["groundDragFactor"].AsDouble(1.0);
		}
	}

	public override bool Applicable(Entity entity, EntityPos pos, EntityControls controls)
	{
		int num;
		if (entity.OnGround)
		{
			num = ((!entity.Swimming) ? 1 : 0);
			if (num != 0 && antiCoyoteTimer <= 0f)
			{
				coyoteTimer = 0.15f;
			}
		}
		else
		{
			num = 0;
		}
		if (coyoteTimer > 0f && entity.Attributes.GetInt("dmgkb") > 0)
		{
			coyoteTimer = 0f;
			antiCoyoteTimer = 0.16f;
		}
		if (num == 0)
		{
			return coyoteTimer > 0f;
		}
		return true;
	}

	public override void DoApply(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		coyoteTimer -= dt;
		antiCoyoteTimer = Math.Max(0f, antiCoyoteTimer - dt);
		Block belowBlock = entity.World.BlockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY - 0.05000000074505806), (int)pos.Z);
		accum = Math.Min(1f, accum + dt);
		float frameTime = 1f / 60f;
		while (accum > frameTime)
		{
			accum -= frameTime;
			if (entity.Alive)
			{
				double multiplier = (entity as EntityAgent).GetWalkSpeedMultiplier(groundDragFactor);
				motionDelta.Set(motionDelta.X + (controls.WalkVector.X * multiplier - motionDelta.X) * (double)belowBlock.DragMultiplier, 0.0, motionDelta.Z + (controls.WalkVector.Z * multiplier - motionDelta.Z) * (double)belowBlock.DragMultiplier);
				pos.Motion.Add(motionDelta.X, 0.0, motionDelta.Z);
			}
			double dragStrength = 1.0 - groundDragFactor;
			pos.Motion.X *= dragStrength;
			pos.Motion.Z *= dragStrength;
		}
		if (controls.Jump && entity.World.ElapsedMilliseconds - lastJump > 500 && entity.Alive)
		{
			EntityPlayer entityPlayer = entity as EntityPlayer;
			lastJump = entity.World.ElapsedMilliseconds;
			float jumpHeightMultiplier = MathF.Sqrt(MathF.Max(1f, entityPlayer?.Stats.GetBlended("jumpHeightMul") ?? 1f));
			pos.Motion.Y = GlobalConstants.BaseJumpForce * 1f / 60f * jumpHeightMultiplier;
			IPlayer player = entityPlayer?.World.PlayerByUid(entityPlayer.PlayerUID);
			entity.PlayEntitySound("jump", player, randomizePitch: false);
		}
	}
}
