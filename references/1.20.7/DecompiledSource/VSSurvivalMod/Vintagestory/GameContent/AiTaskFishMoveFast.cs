using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFishMoveFast : AiTaskBase
{
	public Vec3d MainTarget;

	private bool done;

	private float moveSpeed = 0.06f;

	private float wanderChance = 0.04f;

	private float? preferredLightLevel;

	private float targetDistance = 0.12f;

	private NatFloat wanderRangeHorizontal = NatFloat.createStrongerInvexp(3f, 40f);

	private NatFloat wanderRangeVertical = NatFloat.createStrongerInvexp(3f, 10f);

	public bool TeleportWhenOutOfRange = true;

	public double TeleportInGameHours = 1.0;

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

	public AiTaskFishMoveFast(EntityAgent entity)
		: base(entity)
	{
	}

	public override void OnEntityLoaded()
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		float wanderRangeMin = 3f;
		float wanderRangeMax = 30f;
		targetDistance = taskConfig["targetDistance"].AsFloat(0.12f);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		wanderChance = taskConfig["wanderChance"].AsFloat(0.015f);
		wanderRangeMin = taskConfig["wanderRangeMin"].AsFloat(3f);
		wanderRangeMax = taskConfig["wanderRangeMax"].AsFloat(30f);
		wanderRangeHorizontal = NatFloat.createStrongerInvexp(wanderRangeMin, wanderRangeMax);
		preferredLightLevel = taskConfig["preferredLightLevel"].AsFloat(-99f);
		if (preferredLightLevel < 0f)
		{
			preferredLightLevel = null;
		}
	}

	public Vec3d loadNextWanderTarget()
	{
		int tries = 9;
		Vec4d bestTarget = null;
		Vec4d curTarget = new Vec4d();
		BlockPos tmpPos = new BlockPos();
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
				curTarget.W = 1.0 / (Math.Abs(dy) + 1.0);
			}
			if (preferredLightLevel.HasValue && curTarget.W != 0.0)
			{
				tmpPos.Set((int)curTarget.X, (int)curTarget.Y, (int)curTarget.Z);
				int lightdiff = Math.Abs((int)preferredLightLevel.Value - entity.World.BlockAccessor.GetLightLevel(tmpPos, EnumLightLevelType.MaxLight));
				curTarget.W /= Math.Max(1, lightdiff);
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

	public override bool ShouldExecute()
	{
		if (!entity.Swimming)
		{
			return false;
		}
		if (base.rand.NextDouble() > (double)wanderChance && !entity.CollidedHorizontally && !entity.CollidedVertically)
		{
			return false;
		}
		MainTarget = loadNextWanderTarget();
		return MainTarget != null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		done = false;
		pathTraverser.WalkTowards(MainTarget, moveSpeed, targetDistance, OnGoalReached, OnStuck);
	}

	public override bool ContinueExecute(float dt)
	{
		base.ContinueExecute(dt);
		if ((double)MainTarget.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z) < 0.5)
		{
			pathTraverser.Stop();
			return false;
		}
		return !done;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			pathTraverser.Stop();
		}
	}

	private void OnStuck()
	{
		done = true;
	}

	private void OnGoalReached()
	{
		done = true;
	}
}
