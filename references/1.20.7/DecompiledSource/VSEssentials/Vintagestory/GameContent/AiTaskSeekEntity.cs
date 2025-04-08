using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskSeekEntity : AiTaskBaseTargetable
{
	protected Vec3d targetPos;

	private readonly Vec3d ownPos = new Vec3d();

	protected float moveSpeed = 0.02f;

	protected float seekingRange = 25f;

	protected float belowTempSeekingRange = 25f;

	protected float belowTempThreshold = -999f;

	protected float maxFollowTime = 60f;

	protected bool stopNow;

	protected bool active;

	protected float currentFollowTime;

	protected bool alarmHerd;

	protected bool leapAtTarget;

	protected float leapHeightMul = 1f;

	protected string leapAnimationCode = "jump";

	protected float leapChance = 1f;

	protected EnumAttackPattern attackPattern;

	protected EnumAICreatureType? creatureType;

	protected long finishedMs;

	protected bool jumpAnimOn;

	protected long lastSearchTotalMs;

	protected long attackModeBeginTotalMs;

	protected long lastHurtByTargetTotalMs;

	protected float extraTargetDistance;

	protected bool lowTempRangeMode;

	protected bool revengeRangeMode;

	protected bool lastPathfindOk;

	protected int searchWaitMs = 4000;

	protected Vec3d lastGoalReachedPos;

	protected Dictionary<long, int> futilityCounters;

	private float executionChance;

	private long jumpedMS;

	private float lastPathUpdateSeconds;

	public float NowSeekRange { get; set; }

	protected bool RecentlyHurt => entity.World.ElapsedMilliseconds - lastHurtByTargetTotalMs < 10000;

	protected bool RemainInOffensiveMode => entity.World.ElapsedMilliseconds - attackModeBeginTotalMs < 20000;

	public AiTaskSeekEntity(EntityAgent entity)
		: base(entity)
	{
		base.Id = "seekentity";
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		tamingGenerations = taskConfig["tamingGenerations"].AsFloat(10f);
		JsonObject leapCodeRaw = taskConfig["leapAnimation"];
		leapAnimationCode = ((leapCodeRaw.Token == null) ? "jump" : leapCodeRaw.AsString());
		leapChance = taskConfig["leapChance"].AsFloat(1f);
		leapHeightMul = taskConfig["leapHeightMul"].AsFloat(1f);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
		extraTargetDistance = taskConfig["extraTargetDistance"].AsFloat();
		seekingRange = taskConfig["seekingRange"].AsFloat(25f);
		belowTempSeekingRange = taskConfig["belowTempSeekingRange"].AsFloat(25f);
		belowTempThreshold = taskConfig["belowTempThreshold"].AsFloat(-999f);
		maxFollowTime = taskConfig["maxFollowTime"].AsFloat(60f);
		alarmHerd = taskConfig["alarmHerd"].AsBool();
		leapAtTarget = taskConfig["leapAtTarget"].AsBool();
		retaliateAttacks = taskConfig["retaliateAttacks"].AsBool(defaultValue: true);
		executionChance = taskConfig["executionChance"].AsFloat(0.1f);
		searchWaitMs = taskConfig["searchWaitMs"].AsInt(4000);
		if (taskConfig["aiCreatureType"].Exists)
		{
			creatureType = (EnumAICreatureType)taskConfig["aiCreatureType"].AsInt();
		}
	}

	public override bool ShouldExecute()
	{
		if (base.noEntityCodes && (attackedByEntity == null || !retaliateAttacks))
		{
			return false;
		}
		if (base.rand.NextDouble() > (double)executionChance && (whenInEmotionState == null || !IsInEmotionState(whenInEmotionState)) && !base.RecentlyAttacked)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (whenInEmotionState == null && base.rand.NextDouble() > 0.5)
		{
			return false;
		}
		if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (jumpAnimOn && entity.World.ElapsedMilliseconds - finishedMs > 2000)
		{
			entity.AnimManager.StopAnimation("jump");
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds && !base.RecentlyAttacked)
		{
			return false;
		}
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		if (!base.RecentlyAttacked)
		{
			attackedByEntity = null;
			revengeRangeMode = false;
		}
		NowSeekRange = getSeekRange();
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, NowSeekRange, ignoreEntityCode: true))
		{
			targetEntity = attackedByEntity;
			targetPos = targetEntity.ServerPos.XYZ;
			revengeRangeMode = true;
			return true;
		}
		bool fullyTamed = (float)GetOwnGeneration() >= tamingGenerations;
		ownPos.SetWithDimension(entity.ServerPos);
		targetEntity = partitionUtil.GetNearestEntity(ownPos, NowSeekRange, (Entity e) => (!fullyTamed || !isNonAttackingPlayer(e)) && IsTargetableEntity(e, NowSeekRange), EnumEntitySearchType.Creatures);
		if (targetEntity != null)
		{
			if (alarmHerd && entity.HerdId > 0)
			{
				entity.World.GetNearestEntity(ownPos, NowSeekRange, NowSeekRange, delegate(Entity e)
				{
					EntityAgent entityAgent = e as EntityAgent;
					if (e.EntityId != entity.EntityId && entityAgent != null && entityAgent.Alive && entityAgent.HerdId == entity.HerdId)
					{
						entityAgent.Notify("seekEntity", targetEntity);
					}
					return false;
				});
			}
			targetPos = targetEntity.ServerPos.XYZ;
			if (entity.ServerPos.SquareDistanceTo(targetPos) <= (double)MinDistanceToTarget())
			{
				return false;
			}
			return true;
		}
		return false;
	}

	protected float getSeekRange()
	{
		float nowRange = seekingRange;
		if (belowTempThreshold > -99f && entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature <= belowTempThreshold)
		{
			nowRange = belowTempSeekingRange;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, NowSeekRange * 1.5f, ignoreEntityCode: true))
		{
			nowRange *= 1.5f;
		}
		return nowRange;
	}

	public float MinDistanceToTarget()
	{
		return extraTargetDistance + Math.Max(0.1f, targetEntity.SelectionBox.XSize / 2f + entity.SelectionBox.XSize / 4f);
	}

	public override void StartExecute()
	{
		stopNow = false;
		active = true;
		currentFollowTime = 0f;
		attackPattern = EnumAttackPattern.DirectAttack;
		int searchDepth = 3500;
		if (world.Rand.NextDouble() < 0.05)
		{
			searchDepth = 10000;
		}
		pathTraverser.NavigateTo_Async(targetPos.Clone(), moveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck, OnSeekUnable, searchDepth, 1, creatureType);
	}

	private void OnSeekUnable()
	{
		OnSiegeUnable();
	}

	private void OnSiegeUnable()
	{
		if (targetPos.DistanceTo(entity.ServerPos.XYZ) < NowSeekRange && !TryCircleTarget())
		{
			OnCircleTargetUnable();
		}
	}

	public void OnCircleTargetUnable()
	{
		entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.AllTasks.ForEach(delegate(IAiTask t)
		{
			(t as AiTaskFleeEntity)?.InstaFleeFrom(targetEntity);
		});
	}

	private bool TryCircleTarget()
	{
		targetPos.SquareDistanceTo(entity.Pos);
		int searchDepth = 3500;
		attackPattern = EnumAttackPattern.CircleTarget;
		lastPathfindOk = false;
		float angle = (float)Math.Atan2(entity.ServerPos.X - targetPos.X, entity.ServerPos.Z - targetPos.Z);
		for (int i = 0; i < 3; i++)
		{
			double value = (double)angle + 0.5 + world.Rand.NextDouble() / 2.0;
			double distance = 4.0 + world.Rand.NextDouble() * 6.0;
			double dx = GameMath.Sin(value) * distance;
			double dz = GameMath.Cos(value) * distance;
			targetPos.Add(dx, 0.0, dz);
			int tries = 0;
			bool ok = false;
			BlockPos tmp = new BlockPos((int)targetPos.X, (int)targetPos.Y, (int)targetPos.Z);
			int dy = 0;
			while (tries < 5)
			{
				if (world.BlockAccessor.GetBlockBelow(tmp, dy).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, new Vec3d((double)tmp.X + 0.5, tmp.Y - dy + 1, (double)tmp.Z + 0.5), alsoCheckTouch: false))
				{
					ok = true;
					targetPos.Y -= dy;
					targetPos.Y += 1.0;
					break;
				}
				if (world.BlockAccessor.GetBlockAbove(tmp, dy).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, new Vec3d((double)tmp.X + 0.5, tmp.Y + dy + 1, (double)tmp.Z + 0.5), alsoCheckTouch: false))
				{
					ok = true;
					targetPos.Y += dy;
					targetPos.Y += 1.0;
					break;
				}
				tries++;
				dy++;
			}
			if (ok)
			{
				pathTraverser.NavigateTo_Async(targetPos.Clone(), moveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck, OnCircleTargetUnable, searchDepth, 1);
				return true;
			}
		}
		return false;
	}

	public override bool CanContinueExecute()
	{
		if (pathTraverser.Ready)
		{
			attackModeBeginTotalMs = entity.World.ElapsedMilliseconds;
			lastPathfindOk = true;
		}
		return pathTraverser.Ready;
	}

	private double getDistanceToTarget()
	{
		if (targetEntity == null)
		{
			return double.MaxValue;
		}
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		return cuboidd.ShortestDistanceFrom(pos);
	}

	public override bool ContinueExecute(float dt)
	{
		if (currentFollowTime == 0f && (!stopNow || world.Rand.NextDouble() < 0.25))
		{
			base.StartExecute();
		}
		currentFollowTime += dt;
		lastPathUpdateSeconds += dt;
		double distance = getDistanceToTarget();
		if (attackPattern == EnumAttackPattern.DirectAttack && lastPathUpdateSeconds >= 0.75f && targetPos.SquareDistanceTo(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z) >= 9f)
		{
			targetPos.Set(targetEntity.ServerPos.X + targetEntity.ServerPos.Motion.X * 10.0, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z + targetEntity.ServerPos.Motion.Z * 10.0);
			pathTraverser.NavigateTo(targetPos, moveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck, null, giveUpWhenNoPath: false, 2000, 1);
			lastPathUpdateSeconds = 0f;
		}
		if (leapAtTarget && !entity.AnimManager.IsAnimationActive(animMeta.Code))
		{
			RunningAnimation animationState = entity.AnimManager.Animator.GetAnimationState(leapAnimationCode);
			if (animationState == null || !animationState.Active)
			{
				animMeta.EaseInSpeed = 1f;
				animMeta.EaseOutSpeed = 1f;
				entity.AnimManager.StartAnimation(animMeta);
			}
		}
		if (jumpAnimOn && entity.World.ElapsedMilliseconds - finishedMs > 2000)
		{
			entity.AnimManager.StopAnimation(leapAnimationCode);
			animMeta.EaseInSpeed = 1f;
			animMeta.EaseOutSpeed = 1f;
			entity.AnimManager.StartAnimation(animMeta);
		}
		if (attackPattern == EnumAttackPattern.DirectAttack)
		{
			pathTraverser.CurrentTarget.X = targetEntity.ServerPos.X;
			pathTraverser.CurrentTarget.Y = targetEntity.ServerPos.Y;
			pathTraverser.CurrentTarget.Z = targetEntity.ServerPos.Z;
		}
		EntityPlayer obj = targetEntity as EntityPlayer;
		bool inCreativeMode = obj != null && (obj.Player?.WorldData.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative;
		if (!inCreativeMode && leapAtTarget && base.rand.NextDouble() < (double)leapChance)
		{
			bool recentlyJumped = entity.World.ElapsedMilliseconds - jumpedMS < 3000;
			if (distance > 0.5 && distance < 4.0 && !recentlyJumped && targetEntity.ServerPos.Y + 0.1 >= entity.ServerPos.Y)
			{
				double dx = (targetEntity.ServerPos.X + targetEntity.ServerPos.Motion.X * 80.0 - entity.ServerPos.X) / 30.0;
				double dz = (targetEntity.ServerPos.Z + targetEntity.ServerPos.Motion.Z * 80.0 - entity.ServerPos.Z) / 30.0;
				entity.ServerPos.Motion.Add(dx, (double)leapHeightMul * GameMath.Max(0.13, (targetEntity.ServerPos.Y - entity.ServerPos.Y) / 30.0), dz);
				float yaw = (float)Math.Atan2(dx, dz);
				entity.ServerPos.Yaw = yaw;
				jumpedMS = entity.World.ElapsedMilliseconds;
				finishedMs = entity.World.ElapsedMilliseconds;
				if (leapAnimationCode != null)
				{
					entity.AnimManager.StopAnimation("walk");
					entity.AnimManager.StopAnimation("run");
					entity.AnimManager.StartAnimation(new AnimationMetaData
					{
						Animation = leapAnimationCode,
						Code = leapAnimationCode
					}.Init());
					jumpAnimOn = true;
				}
			}
			if (recentlyJumped && !entity.Collided && distance < 0.5)
			{
				entity.ServerPos.Motion /= 2f;
			}
		}
		float minDist = MinDistanceToTarget();
		if (targetEntity.Alive && !stopNow && !inCreativeMode && pathTraverser.Active && currentFollowTime < maxFollowTime && distance < (double)NowSeekRange)
		{
			if (!(distance > (double)minDist))
			{
				if (targetEntity is EntityAgent ea)
				{
					return ea.ServerControls.TriesToMove;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		finishedMs = entity.World.ElapsedMilliseconds;
		pathTraverser.Stop();
		active = false;
	}

	public override bool Notify(string key, object data)
	{
		if (key == "seekEntity")
		{
			targetEntity = (Entity)data;
			targetPos = targetEntity.ServerPos.XYZ;
			return true;
		}
		return false;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		base.OnEntityHurt(source, damage);
		if (targetEntity != source.GetCauseEntity() && active)
		{
			return;
		}
		lastHurtByTargetTotalMs = entity.World.ElapsedMilliseconds;
		float num = ((source.GetCauseEntity() == null) ? 0f : ((float)source.GetCauseEntity().ServerPos.DistanceTo(entity.ServerPos)));
		float seekrange = getSeekRange();
		if (num > seekrange)
		{
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.AllTasks.ForEach(delegate(IAiTask t)
			{
				(t as AiTaskFleeEntity)?.InstaFleeFrom(targetEntity);
			});
		}
	}

	private void OnStuck()
	{
		stopNow = true;
	}

	private void OnGoalReached()
	{
		if (attackPattern != 0)
		{
			return;
		}
		if (lastGoalReachedPos != null && (double)lastGoalReachedPos.SquareDistanceTo(entity.Pos) < 0.001)
		{
			if (futilityCounters == null)
			{
				futilityCounters = new Dictionary<long, int>();
			}
			else
			{
				futilityCounters.TryGetValue(targetEntity.EntityId, out var futilityCounter);
				futilityCounter++;
				futilityCounters[targetEntity.EntityId] = futilityCounter;
				if (futilityCounter > 19)
				{
					return;
				}
			}
		}
		lastGoalReachedPos = new Vec3d(entity.Pos);
		pathTraverser.Retarget();
	}

	public override bool CanSense(Entity e, double range)
	{
		bool result = base.CanSense(e, range);
		if (result && futilityCounters != null && futilityCounters.TryGetValue(e.EntityId, out var futilityCounter) && futilityCounter > 0)
		{
			futilityCounter -= 2;
			futilityCounters[e.EntityId] = futilityCounter;
			return false;
		}
		return result;
	}
}
