using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskStayInRange : AiTaskBaseTargetable
{
	protected Vec3d targetPos;

	private readonly Vec3d ownPos = new Vec3d();

	protected float moveSpeed = 0.02f;

	protected float searchRange = 25f;

	protected float targetRange = 15f;

	protected float rangeTolerance = 2f;

	protected bool stopNow;

	protected bool active;

	protected float currentFollowTime;

	protected long finishedMs;

	protected long lastSearchTotalMs;

	protected long lastHurtByTargetTotalMs;

	protected bool lastPathfindOk;

	protected int searchWaitMs = 4000;

	protected Vec3d lastGoalReachedPos;

	protected Dictionary<long, int> futilityCounters;

	private float executionChance;

	private long jumpedMS;

	private float lastPathUpdateSeconds;

	protected bool RecentlyHurt => entity.World.ElapsedMilliseconds - lastHurtByTargetTotalMs < 10000;

	public AiTaskStayInRange(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
		searchRange = taskConfig["searchRange"].AsFloat(25f);
		targetRange = taskConfig["targetRange"].AsFloat(15f);
		rangeTolerance = taskConfig["targetRangeTolerance"].AsFloat(2f);
		retaliateAttacks = taskConfig["retaliateAttacks"].AsBool(defaultValue: true);
		executionChance = taskConfig["executionChance"].AsFloat(0.1f);
		searchWaitMs = taskConfig["searchWaitMs"].AsInt(4000);
	}

	public override bool ShouldExecute()
	{
		if (base.noEntityCodes && (attackedByEntity == null || !retaliateAttacks))
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (targetEntity != null)
		{
			float num = entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos);
			bool toofar2 = num > (targetRange + rangeTolerance) * (targetRange + rangeTolerance);
			bool toonear = num < (targetRange - rangeTolerance) * (targetRange - rangeTolerance);
			if (toofar2 || toonear)
			{
				return true;
			}
		}
		if (whenInEmotionState == null && base.rand.NextDouble() > 0.5)
		{
			return false;
		}
		if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds && !base.RecentlyAttacked)
		{
			return false;
		}
		if (base.rand.NextDouble() > (double)executionChance && (whenInEmotionState == null || !IsInEmotionState(whenInEmotionState)) && !base.RecentlyAttacked)
		{
			return false;
		}
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		if (!base.RecentlyAttacked)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, searchRange, ignoreEntityCode: true))
		{
			targetEntity = attackedByEntity;
			targetPos = targetEntity.ServerPos.XYZ;
			return true;
		}
		ownPos.SetWithDimension(entity.ServerPos);
		targetEntity = partitionUtil.GetNearestEntity(ownPos, searchRange, (Entity e) => IsTargetableEntity(e, searchRange), EnumEntitySearchType.Creatures);
		if (targetEntity != null)
		{
			targetPos = targetEntity.ServerPos.XYZ;
			double num2 = entity.ServerPos.SquareDistanceTo(targetPos);
			bool toofar = num2 > (double)((targetRange + rangeTolerance) * (targetRange + rangeTolerance));
			return num2 < (double)((targetRange - rangeTolerance) * (targetRange - rangeTolerance)) || toofar;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		stopNow = false;
		active = true;
		currentFollowTime = 0f;
	}

	public override bool CanContinueExecute()
	{
		return true;
	}

	public override bool ContinueExecute(float dt)
	{
		if (pathTraverser.Active)
		{
			return true;
		}
		float num = entity.ServerPos.SquareDistanceTo(targetEntity.ServerPos);
		bool toofar = num > (targetRange + rangeTolerance) * (targetRange + rangeTolerance);
		bool toonear = num < (targetRange - rangeTolerance) * (targetRange - rangeTolerance);
		bool canWalk = false;
		if (toofar)
		{
			canWalk = WalkTowards(-1);
		}
		else if (toonear)
		{
			canWalk = WalkTowards(1);
		}
		if (canWalk)
		{
			return toofar || toonear;
		}
		return false;
	}

	private bool WalkTowards(int sign)
	{
		_ = entity.World.BlockAccessor;
		Vec3d selfpos = entity.ServerPos.XYZ;
		Vec3d dir = selfpos.SubCopy(targetEntity.ServerPos.X, selfpos.Y, targetEntity.ServerPos.Z).Normalize();
		Vec3d nextPos = selfpos + sign * dir;
		Vec3d testPos = new Vec3d((double)(int)nextPos.X + 0.5, (int)nextPos.Y, (double)(int)nextPos.Z + 0.5);
		if (canStepTowards(nextPos))
		{
			pathTraverser.WalkTowards(nextPos, moveSpeed, 0.3f, OnGoalReached, OnStuck);
			return true;
		}
		int rnds = 1 - entity.World.Rand.Next(2) * 2;
		Vec3d ldir = dir.RotatedCopy((float)rnds * ((float)Math.PI / 2f));
		nextPos = selfpos + ldir;
		testPos = new Vec3d((double)(int)nextPos.X + 0.5, (int)nextPos.Y, (double)(int)nextPos.Z + 0.5);
		if (canStepTowards(testPos))
		{
			pathTraverser.WalkTowards(nextPos, moveSpeed, 0.3f, OnGoalReached, OnStuck);
			return true;
		}
		Vec3d rdir = dir.RotatedCopy((float)(-rnds) * ((float)Math.PI / 2f));
		nextPos = selfpos + rdir;
		testPos = new Vec3d((double)(int)nextPos.X + 0.5, (int)nextPos.Y, (double)(int)nextPos.Z + 0.5);
		if (canStepTowards(testPos))
		{
			pathTraverser.WalkTowards(nextPos, moveSpeed, 0.3f, OnGoalReached, OnStuck);
			return true;
		}
		return false;
	}

	private bool canStepTowards(Vec3d nextPos)
	{
		bool hereCollide = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, nextPos, alsoCheckTouch: false);
		if (hereCollide && !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, collTmpVec.Set(nextPos).Add(0.0, Math.Min(1f, stepHeight), 0.0), alsoCheckTouch: false))
		{
			return true;
		}
		if (hereCollide)
		{
			return false;
		}
		if (isLiquidAt(nextPos))
		{
			return false;
		}
		bool belowCollide = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, collTmpVec.Set(nextPos).Add(0.0, -1.1, 0.0), alsoCheckTouch: false);
		if (belowCollide)
		{
			nextPos.Y -= 1.0;
			return true;
		}
		if (isLiquidAt(collTmpVec))
		{
			return false;
		}
		bool below2Collide = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, collTmpVec.Set(nextPos).Add(0.0, -2.1, 0.0), alsoCheckTouch: false);
		if (!belowCollide && below2Collide && entity.ServerPos.Y - base.TargetEntity.ServerPos.Y >= 1.0)
		{
			nextPos.Y -= 2.0;
			return true;
		}
		if (isLiquidAt(collTmpVec))
		{
			return false;
		}
		bool below3Collide = world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, collTmpVec.Set(nextPos).Add(0.0, -3.1, 0.0), alsoCheckTouch: false);
		if (!belowCollide && !below2Collide && below3Collide && entity.ServerPos.Y - base.TargetEntity.ServerPos.Y >= 2.0)
		{
			nextPos.Y -= 3.0;
			return true;
		}
		return false;
	}

	protected bool isLiquidAt(Vec3d pos)
	{
		return entity.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z).IsLiquid();
	}

	private void WalkTowards()
	{
		Vec3d selfpos = entity.ServerPos.XYZ;
		Vec3d dir = selfpos.Sub(targetEntity.ServerPos.XYZ).Normalize();
		pathTraverser.WalkTowards(selfpos + dir, moveSpeed, 0.25f, OnGoalReached, OnStuck);
	}

	private void OnStuck()
	{
	}

	private void OnGoalReached()
	{
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		finishedMs = entity.World.ElapsedMilliseconds;
		active = false;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		base.OnEntityHurt(source, damage);
		if (targetEntity == source.GetCauseEntity() || !active)
		{
			lastHurtByTargetTotalMs = entity.World.ElapsedMilliseconds;
			if (targetEntity != null)
			{
				targetEntity.ServerPos.DistanceTo(entity.ServerPos);
			}
		}
	}
}
