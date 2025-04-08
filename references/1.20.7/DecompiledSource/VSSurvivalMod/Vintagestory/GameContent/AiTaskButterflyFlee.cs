using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskButterflyFlee : AiTaskButterflyWander
{
	internal Entity fleeFromEntity;

	internal Vec3d targetPos = new Vec3d();

	protected float fleeTime;

	protected bool fleeState;

	protected float seekingRange = 5f;

	public JsonObject taskConfig;

	private Vec3d tmpVec = new Vec3d();

	public AiTaskButterflyFlee(EntityAgent entity)
		: base(entity)
	{
	}

	public AiTaskButterflyFlee(EntityAgent entity, EntityButterfly chaseTarget)
		: base(entity)
	{
		fleeTime = (float)entity.World.Rand.NextDouble() * 7f + 6f;
		fleeFromEntity = chaseTarget;
		targetPos.Set(fleeFromEntity.ServerPos.X, fleeFromEntity.ServerPos.Y, fleeFromEntity.ServerPos.Z);
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		this.taskConfig = taskConfig;
		base.LoadConfig(taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() > 0.05)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		fleeFromEntity = entity.World.GetNearestEntity(entity.ServerPos.XYZ, seekingRange, seekingRange, delegate(Entity e)
		{
			if (!e.Alive || e.EntityId == entity.EntityId)
			{
				return false;
			}
			if (e is EntityPlayer entityPlayer && !entityPlayer.ServerControls.Sneak)
			{
				IPlayer player = entityPlayer.Player;
				if (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					IPlayer player2 = entityPlayer.Player;
					if (player2 == null || player2.WorldData.CurrentGameMode != EnumGameMode.Spectator)
					{
						return true;
					}
				}
			}
			return false;
		});
		if (fleeFromEntity != null)
		{
			fleeTime = (float)entity.World.Rand.NextDouble() * 3.5f + 3f;
			updateTargetPos();
			return true;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		pathTraverser.WalkTowards(targetPos, moveSpeed, 0.0001f, OnGoalReached, OnStuck);
	}

	private void OnStuck()
	{
	}

	private void OnGoalReached()
	{
		fleeState = !fleeState;
		if (fleeState)
		{
			pathTraverser.WalkTowards(targetPos.Add(0.0, 1.0, 0.0), moveSpeed, 1f, OnGoalReached, OnStuck);
		}
		else
		{
			pathTraverser.WalkTowards(targetPos, moveSpeed, 1f, OnGoalReached, OnStuck);
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (world.Rand.NextDouble() < 0.2)
		{
			updateTargetPos();
			pathTraverser.CurrentTarget.X = targetPos.X;
			pathTraverser.CurrentTarget.Y = targetPos.Y;
			pathTraverser.CurrentTarget.Z = targetPos.Z;
		}
		if (entity.ServerPos.SquareDistanceTo(fleeFromEntity.ServerPos.XYZ) > 25.0)
		{
			return false;
		}
		return (fleeTime -= dt) >= 0f;
	}

	private void updateTargetPos()
	{
		float yaw = (float)Math.Atan2(fleeFromEntity.ServerPos.X - entity.ServerPos.X, fleeFromEntity.ServerPos.Z - entity.ServerPos.Z);
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw + (float)Math.PI / 2f);
		if (traversable(tmpVec))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 3f, yaw + (float)Math.PI / 2f);
			return;
		}
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw + (float)Math.PI);
		if (traversable(tmpVec))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 3f, yaw + (float)Math.PI);
			return;
		}
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, yaw);
		if (traversable(tmpVec))
		{
			targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 3f, yaw);
			return;
		}
		tmpVec = tmpVec.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		tmpVec.Ahead(0.9, 0f, 0f - yaw);
		targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(10.0, 0f, 0f - yaw);
	}

	private bool traversable(Vec3d pos)
	{
		return !world.CollisionTester.IsColliding(world.BlockAccessor, entity.SelectionBox, pos, alsoCheckTouch: false);
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}
}
