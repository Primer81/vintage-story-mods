using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.Essentials;

public class WaypointsTraverser : PathTraverserBase
{
	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private Vec3f targetVec = new Vec3f();

	private List<Vec3d> waypoints;

	private List<Vec3d> newWaypoints;

	private PathfinderTask asyncSearchObject;

	private int waypointToReachIndex;

	private long lastWaypointIncTotalMs;

	private Vec3d desiredTarget;

	private PathfindSystem psys;

	private PathfindingAsync asyncPathfinder;

	protected EnumAICreatureType creatureType;

	public bool PathFindDebug;

	private Action OnNoPath;

	public Action OnFoundPath;

	private Action OnGoalReached_New;

	private Action OnStuck_New;

	private float movingSpeed_New;

	private float targetDistance_New;

	private Vec3d prevPos = new Vec3d(0.0, -2000.0, 0.0);

	private Vec3d prevPrevPos = new Vec3d(0.0, -1000.0, 0.0);

	private float prevPosAccum;

	private float sqDistToTarget;

	private float distCheckAccum;

	private float lastDistToTarget;

	public override Vec3d CurrentTarget => waypoints[waypoints.Count - 1];

	public override bool Ready
	{
		get
		{
			if (waypoints != null)
			{
				return asyncSearchObject == null;
			}
			return false;
		}
	}

	public WaypointsTraverser(EntityAgent entity, EnumAICreatureType creatureType = EnumAICreatureType.Default)
		: base(entity)
	{
		if (entity?.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder") != null)
		{
			minTurnAnglePerSec = (float)entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetDecimal("minTurnAnglePerSec", 250.0);
			maxTurnAnglePerSec = (float)entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetDecimal("maxTurnAnglePerSec", 450.0);
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
		psys = entity.World.Api.ModLoader.GetModSystem<PathfindSystem>();
		asyncPathfinder = entity.World.Api.ModLoader.GetModSystem<PathfindingAsync>();
		this.creatureType = creatureType;
	}

	public void FollowRoute(List<Vec3d> swoopPath, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck)
	{
		waypoints = swoopPath;
		base.WalkTowards(desiredTarget, movingSpeed, targetDistance, OnGoalReached, OnStuck);
	}

	public override bool NavigateTo(Vec3d target, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck, Action onNoPath = null, bool giveUpWhenNoPath = false, int searchDepth = 999, int mhdistanceTolerance = 0, EnumAICreatureType? creatureType = null)
	{
		desiredTarget = target;
		OnNoPath = onNoPath;
		OnStuck_New = OnStuck;
		OnGoalReached_New = OnGoalReached;
		movingSpeed_New = movingSpeed;
		targetDistance_New = targetDistance;
		if (creatureType.HasValue)
		{
			this.creatureType = creatureType.Value;
		}
		BlockPos startBlockPos = entity.ServerPos.AsBlockPos;
		if (entity.World.BlockAccessor.IsNotTraversable(startBlockPos))
		{
			HandleNoPath();
			return false;
		}
		FindPath(startBlockPos, target.AsBlockPos, searchDepth, mhdistanceTolerance);
		return AfterFoundPath();
	}

	public override bool NavigateTo_Async(Vec3d target, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck, Action onNoPath = null, int searchDepth = 999, int mhdistanceTolerance = 0, EnumAICreatureType? creatureType = null)
	{
		if (asyncSearchObject != null)
		{
			return false;
		}
		desiredTarget = target;
		if (creatureType.HasValue)
		{
			this.creatureType = creatureType.Value;
		}
		OnNoPath = onNoPath;
		OnGoalReached_New = OnGoalReached;
		OnStuck_New = OnStuck;
		movingSpeed_New = movingSpeed;
		targetDistance_New = targetDistance;
		BlockPos startBlockPos = entity.ServerPos.AsBlockPos;
		if (entity.World.BlockAccessor.IsNotTraversable(startBlockPos))
		{
			HandleNoPath();
			return false;
		}
		FindPath_Async(startBlockPos, target.AsBlockPos, searchDepth, mhdistanceTolerance);
		return true;
	}

	private void FindPath(BlockPos startBlockPos, BlockPos targetBlockPos, int searchDepth, int mhdistanceTolerance = 0)
	{
		waypointToReachIndex = 0;
		float stepHeight = entity.GetBehavior<EntityBehaviorControlledPhysics>()?.StepHeight ?? 0.6f;
		int maxFallHeight = (entity.Properties.FallDamage ? (Math.Min(8, (int)Math.Round(3.51 / Math.Max(0.01, entity.Properties.FallDamageMultiplier))) - (int)(movingSpeed * 30f)) : 8);
		newWaypoints = psys.FindPathAsWaypoints(startBlockPos, targetBlockPos, maxFallHeight, stepHeight, entity.CollisionBox, searchDepth, mhdistanceTolerance, creatureType);
	}

	public PathfinderTask PreparePathfinderTask(BlockPos startBlockPos, BlockPos targetBlockPos, int searchDepth = 999, int mhdistanceTolerance = 0, EnumAICreatureType? creatureType = null)
	{
		float stepHeight = entity.GetBehavior<EntityBehaviorControlledPhysics>()?.StepHeight ?? 0.6f;
		int num;
		if (entity.Properties.FallDamage)
		{
			JsonObject attributes = entity.Properties.Attributes;
			if (attributes == null || !attributes["reckless"].AsBool())
			{
				num = 4 - (int)(movingSpeed * 30f);
				goto IL_0071;
			}
		}
		num = 12;
		goto IL_0071;
		IL_0071:
		int maxFallHeight = num;
		return new PathfinderTask(startBlockPos, targetBlockPos, maxFallHeight, stepHeight, entity.CollisionBox, searchDepth, mhdistanceTolerance, creatureType ?? this.creatureType);
	}

	private void FindPath_Async(BlockPos startBlockPos, BlockPos targetBlockPos, int searchDepth, int mhdistanceTolerance = 0)
	{
		waypointToReachIndex = 0;
		asyncSearchObject = PreparePathfinderTask(startBlockPos, targetBlockPos, searchDepth, mhdistanceTolerance, creatureType);
		asyncPathfinder.EnqueuePathfinderTask(asyncSearchObject);
	}

	public bool AfterFoundPath()
	{
		if (asyncSearchObject != null)
		{
			newWaypoints = asyncSearchObject.waypoints;
			asyncSearchObject = null;
		}
		if (newWaypoints == null)
		{
			HandleNoPath();
			return false;
		}
		waypoints = newWaypoints;
		if (PathFindDebug)
		{
			List<BlockPos> poses = new List<BlockPos>();
			List<int> colors = new List<int>();
			int i = 0;
			foreach (Vec3d node in waypoints)
			{
				poses.Add(node.AsBlockPos);
				colors.Add(ColorUtil.ColorFromRgba(128, 128, Math.Min(255, 128 + i * 8), 150));
				i++;
			}
			poses.Add(desiredTarget.AsBlockPos);
			colors.Add(ColorUtil.ColorFromRgba(128, 0, 255, 255));
			IPlayer player = entity.World.AllOnlinePlayers[0];
			entity.World.HighlightBlocks(player, 2, poses, colors);
		}
		waypoints.Add(desiredTarget);
		base.WalkTowards(desiredTarget, movingSpeed_New, targetDistance_New, OnGoalReached_New, OnStuck_New);
		OnFoundPath?.Invoke();
		return true;
	}

	public void HandleNoPath()
	{
		waypoints = new List<Vec3d>();
		if (PathFindDebug)
		{
			List<BlockPos> poses = new List<BlockPos>();
			List<int> colors = new List<int>();
			int i = 0;
			foreach (PathNode node in entity.World.Api.ModLoader.GetModSystem<PathfindSystem>().astar.closedSet)
			{
				poses.Add(node);
				colors.Add(ColorUtil.ColorFromRgba(Math.Min(255, i * 4), 0, 0, 150));
				i++;
			}
			IPlayer player = entity.World.AllOnlinePlayers[0];
			entity.World.HighlightBlocks(player, 2, poses, colors);
		}
		waypoints.Add(desiredTarget);
		base.WalkTowards(desiredTarget, movingSpeed_New, targetDistance_New, OnGoalReached_New, OnStuck_New);
		if (OnNoPath != null)
		{
			Active = false;
			OnNoPath();
		}
	}

	public override bool WalkTowards(Vec3d target, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck, EnumAICreatureType creatureType = EnumAICreatureType.Default)
	{
		waypoints = new List<Vec3d>();
		waypoints.Add(target);
		return base.WalkTowards(target, movingSpeed, targetDistance, OnGoalReached, OnStuck, creatureType);
	}

	protected override bool BeginGo()
	{
		entity.Controls.Forward = true;
		entity.ServerControls.Forward = true;
		curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= 0.87266463f;
		stuckCounter = 0;
		waypointToReachIndex = 0;
		lastWaypointIncTotalMs = entity.World.ElapsedMilliseconds;
		distCheckAccum = 0f;
		prevPosAccum = 0f;
		return true;
	}

	public override void OnGameTick(float dt)
	{
		if (asyncSearchObject != null)
		{
			if (!asyncSearchObject.Finished)
			{
				return;
			}
			AfterFoundPath();
		}
		if (!Active)
		{
			return;
		}
		bool nearHorizontally = false;
		int offset = 0;
		bool nearAllDirs = IsNearTarget(offset++, ref nearHorizontally) || IsNearTarget(offset++, ref nearHorizontally) || IsNearTarget(offset++, ref nearHorizontally);
		if (nearAllDirs)
		{
			waypointToReachIndex += offset;
			lastWaypointIncTotalMs = entity.World.ElapsedMilliseconds;
		}
		target = waypoints[Math.Min(waypoints.Count - 1, waypointToReachIndex)];
		_ = waypointToReachIndex;
		_ = waypoints.Count;
		if (waypointToReachIndex >= waypoints.Count)
		{
			Stop();
			OnGoalReached?.Invoke();
			return;
		}
		bool stuckBelowOrAbove = nearHorizontally && !nearAllDirs && entity.Properties.Habitat == EnumHabitat.Land;
		bool stuck = (entity.CollidedVertically && entity.Controls.IsClimbing) || (entity.CollidedHorizontally && entity.ServerPos.Motion.Y <= 0.0) || stuckBelowOrAbove || (entity.CollidedHorizontally && waypoints.Count > 1 && waypointToReachIndex < waypoints.Count && entity.World.ElapsedMilliseconds - lastWaypointIncTotalMs > 2000);
		double distsq = prevPrevPos.SquareDistanceTo(prevPos);
		stuck |= distsq < 0.0001 && entity.World.Rand.NextDouble() < GameMath.Clamp(1.0 - distsq * 1.2, 0.1, 0.9);
		prevPosAccum += dt;
		if ((double)prevPosAccum > 0.2)
		{
			prevPosAccum = 0f;
			prevPrevPos.Set(prevPos);
			prevPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		}
		distCheckAccum += dt;
		if (distCheckAccum > 2f)
		{
			distCheckAccum = 0f;
			if ((double)Math.Abs(sqDistToTarget - lastDistToTarget) < 0.1)
			{
				stuck = true;
				stuckCounter += 30;
			}
			else if (!stuck)
			{
				stuckCounter = 0;
			}
			lastDistToTarget = sqDistToTarget;
		}
		if (stuck)
		{
			stuckCounter++;
		}
		if (GlobalConstants.OverallSpeedMultiplier > 0f && (float)stuckCounter > 60f / GlobalConstants.OverallSpeedMultiplier)
		{
			Stop();
			OnStuck?.Invoke();
			return;
		}
		EntityControls controls = ((entity.MountedOn == null) ? entity.Controls : entity.MountedOn.Controls);
		if (controls == null)
		{
			return;
		}
		targetVec.Set((float)(target.X - entity.ServerPos.X), (float)(target.Y - entity.ServerPos.Y), (float)(target.Z - entity.ServerPos.Z));
		targetVec.Normalize();
		float desiredYaw = 0f;
		if ((double)sqDistToTarget >= 0.01)
		{
			desiredYaw = (float)Math.Atan2(targetVec.X, targetVec.Z);
		}
		float nowMoveSpeed = movingSpeed;
		if (sqDistToTarget < 1f)
		{
			nowMoveSpeed = Math.Max(0.005f, movingSpeed * Math.Max(sqDistToTarget, 0.2f));
		}
		float yawDist = GameMath.AngleRadDistance(entity.ServerPos.Yaw, desiredYaw);
		float turnSpeed = curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier * movingSpeed;
		entity.ServerPos.Yaw += GameMath.Clamp(yawDist, 0f - turnSpeed, turnSpeed);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		double cosYaw = Math.Cos(entity.ServerPos.Yaw);
		double sinYaw = Math.Sin(entity.ServerPos.Yaw);
		controls.WalkVector.Set(sinYaw, GameMath.Clamp(targetVec.Y, -1f, 1f), cosYaw);
		controls.WalkVector.Mul(nowMoveSpeed * GlobalConstants.OverallSpeedMultiplier / Math.Max(1f, Math.Abs(yawDist) * 3f));
		if (entity.Properties.RotateModelOnClimb && entity.Controls.IsClimbing && entity.ClimbingIntoFace != null && entity.Alive)
		{
			BlockFacing climbingIntoFace = entity.ClimbingIntoFace;
			if (Math.Sign(climbingIntoFace.Normali.X) == Math.Sign(controls.WalkVector.X))
			{
				controls.WalkVector.X = 0.0;
			}
			if (Math.Sign(climbingIntoFace.Normali.Y) == Math.Sign(controls.WalkVector.Y))
			{
				controls.WalkVector.Y = 0.0 - controls.WalkVector.Y;
			}
			if (Math.Sign(climbingIntoFace.Normali.Z) == Math.Sign(controls.WalkVector.Z))
			{
				controls.WalkVector.Z = 0.0;
			}
		}
		if (entity.Properties.Habitat == EnumHabitat.Underwater)
		{
			controls.FlyVector.Set(controls.WalkVector);
			Vec3d pos = entity.Pos.XYZ;
			Block inblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z, 2);
			Block aboveblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 1.0), (int)pos.Z, 2);
			float swimlineSubmergedness = GameMath.Clamp((float)(int)pos.Y + (float)inblock.LiquidLevel / 8f + (aboveblock.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			swimlineSubmergedness = 1f - Math.Min(1f, swimlineSubmergedness + 0.5f);
			if (swimlineSubmergedness > 0f)
			{
				controls.FlyVector.Y = GameMath.Clamp(controls.FlyVector.Y, -0.03999999910593033, -0.019999999552965164) * (double)(1f - swimlineSubmergedness);
				return;
			}
			float factor = movingSpeed * GlobalConstants.OverallSpeedMultiplier / (float)Math.Sqrt(targetVec.X * targetVec.X + targetVec.Z * targetVec.Z);
			controls.FlyVector.Y = targetVec.Y * factor;
		}
		else if (entity.Swimming)
		{
			controls.FlyVector.Set(controls.WalkVector);
			Vec3d pos2 = entity.Pos.XYZ;
			Block inblock2 = entity.World.BlockAccessor.GetBlock((int)pos2.X, (int)pos2.Y, (int)pos2.Z, 2);
			Block aboveblock2 = entity.World.BlockAccessor.GetBlock((int)pos2.X, (int)(pos2.Y + 1.0), (int)pos2.Z, 2);
			float swimlineSubmergedness2 = GameMath.Clamp((float)(int)pos2.Y + (float)inblock2.LiquidLevel / 8f + (aboveblock2.IsLiquid() ? 1.125f : 0f) - (float)pos2.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			swimlineSubmergedness2 = Math.Min(1f, swimlineSubmergedness2 + 0.5f);
			controls.FlyVector.Y = GameMath.Clamp(controls.FlyVector.Y, 0.019999999552965164, 0.03999999910593033) * (double)swimlineSubmergedness2;
			if (entity.CollidedHorizontally)
			{
				controls.FlyVector.Y = 0.05000000074505806;
			}
		}
	}

	private bool IsNearTarget(int waypointOffset, ref bool nearHorizontally)
	{
		if (waypoints.Count - 1 < waypointToReachIndex + waypointOffset)
		{
			return false;
		}
		int wayPointIndex = Math.Min(waypoints.Count - 1, waypointToReachIndex + waypointOffset);
		Vec3d target = waypoints[wayPointIndex];
		double curPosY = entity.ServerPos.Y;
		sqDistToTarget = target.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z);
		double vdistsq = (target.Y - curPosY) * (target.Y - curPosY);
		bool above = curPosY > target.Y;
		sqDistToTarget += (float)Math.Max(0.0, vdistsq - (above ? 1.0 : 0.5));
		if (!nearHorizontally)
		{
			double horsqDistToTarget = target.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z);
			nearHorizontally = horsqDistToTarget < (double)(TargetDistance * TargetDistance);
		}
		return sqDistToTarget < TargetDistance * TargetDistance;
	}

	private float DiffSquared(double y1, double y2)
	{
		double num = y1 - y2;
		return (float)(num * num);
	}

	public override void Stop()
	{
		Active = false;
		entity.Controls.Forward = false;
		entity.ServerControls.Forward = false;
		entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
		stuckCounter = 0;
		distCheckAccum = 0f;
		prevPosAccum = 0f;
		asyncSearchObject = null;
	}

	public override void Retarget()
	{
		Active = true;
		distCheckAccum = 0f;
		prevPosAccum = 0f;
		waypointToReachIndex = waypoints.Count - 1;
	}
}
