using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFishOutOfWater : AiTaskBase
{
	internal Vec3d targetPos = new Vec3d();

	protected float seekingRange = 2f;

	public JsonObject taskConfig;

	private float moveSpeed = 0.03f;

	private float searchWaterAccum;

	private float outofWaterAccum;

	private NatFloat wanderRangeHorizontal = NatFloat.createStrongerInvexp(3f, 40f);

	private NatFloat wanderRangeVertical = NatFloat.createStrongerInvexp(3f, 10f);

	public float WanderRangeMul
	{
		get
		{
			return entity.Attributes.GetFloat("wanderRangeMul", 1f);
		}
		set
		{
			entity.Attributes.SetFloat("wanderRangeMul", value);
		}
	}

	public int FailedConsecutivePathfinds
	{
		get
		{
			return entity.Attributes.GetInt("failedConsecutivePathfinds");
		}
		set
		{
			entity.Attributes.SetInt("failedConsecutivePathfinds", value);
		}
	}

	public AiTaskFishOutOfWater(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		this.taskConfig = taskConfig;
		base.LoadConfig(taskConfig, aiConfig);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
	}

	public override bool ShouldExecute()
	{
		if (!entity.OnGround || entity.Swimming)
		{
			return false;
		}
		return true;
	}

	private Vec3d nearbyWaterOrRandomTarget()
	{
		int tries = 9;
		Vec4d bestTarget = null;
		Vec4d curTarget = new Vec4d();
		if (FailedConsecutivePathfinds > 10)
		{
			WanderRangeMul = Math.Max(0.1f, WanderRangeMul * 0.9f);
		}
		else
		{
			WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.1f);
			if (base.rand.NextDouble() < 0.05)
			{
				WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.5f);
			}
		}
		float wRangeMul = WanderRangeMul;
		if (base.rand.NextDouble() < 0.05)
		{
			wRangeMul *= 3f;
		}
		while (tries-- > 0)
		{
			double dx = wanderRangeHorizontal.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * wRangeMul;
			double dy = wanderRangeVertical.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * wRangeMul;
			double dz = wanderRangeHorizontal.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * wRangeMul;
			curTarget.X = entity.ServerPos.X + dx;
			curTarget.Y = entity.ServerPos.Y + dy;
			curTarget.Z = entity.ServerPos.Z + dz;
			curTarget.W = 1.0;
			if (!entity.World.BlockAccessor.GetBlock((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z, 2).IsLiquid())
			{
				curTarget.W = 0.0;
			}
			else
			{
				curTarget.W = 1.0 / Math.Sqrt((dx - 1.0) * (dx - 1.0) + (dz - 1.0) * (dz - 1.0) + 1.0);
			}
			if (bestTarget == null || curTarget.W > bestTarget.W)
			{
				bestTarget = new Vec4d(curTarget.X, curTarget.Y, curTarget.Z, curTarget.W);
				if (curTarget.W >= 1.0)
				{
					break;
				}
			}
		}
		if (bestTarget.W > 0.0)
		{
			FailedConsecutivePathfinds = Math.Max(FailedConsecutivePathfinds - 3, 0);
			return bestTarget.XYZ;
		}
		FailedConsecutivePathfinds++;
		return null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		searchWaterAccum = 0f;
		outofWaterAccum = 0f;
		targetPos = nearbyWaterOrRandomTarget();
		if (targetPos != null)
		{
			pathTraverser.WalkTowards(targetPos, moveSpeed, 0.12f, OnGoalReached, OnStuck);
		}
	}

	private void OnStuck()
	{
	}

	private void OnGoalReached()
	{
	}

	public override bool ContinueExecute(float dt)
	{
		if (entity.Swimming)
		{
			return false;
		}
		outofWaterAccum += dt;
		if (outofWaterAccum > 30f)
		{
			entity.Die(EnumDespawnReason.Death, new DamageSource
			{
				Type = EnumDamageType.Suffocation
			});
			return false;
		}
		if (targetPos == null)
		{
			searchWaterAccum += dt;
			if (searchWaterAccum >= 2f)
			{
				targetPos = nearbyWaterOrRandomTarget();
				if (targetPos != null)
				{
					pathTraverser.WalkTowards(targetPos, moveSpeed, 0.12f, OnGoalReached, OnStuck);
				}
				searchWaterAccum = 0f;
			}
		}
		if (targetPos != null && world.Rand.NextDouble() < 0.2)
		{
			pathTraverser.CurrentTarget.X = targetPos.X;
			pathTraverser.CurrentTarget.Y = targetPos.Y;
			pathTraverser.CurrentTarget.Z = targetPos.Z;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}
}
