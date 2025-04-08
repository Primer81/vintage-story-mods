using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskButterflyChase : AiTaskButterflyWander
{
	internal EntityButterfly targetEntity;

	internal Vec3d targetPos = new Vec3d();

	protected float chaseTime;

	protected bool fleeState;

	protected float seekingRange = 3f;

	public JsonObject taskConfig;

	public AiTaskButterflyChase(EntityAgent entity)
		: base(entity)
	{
	}

	public AiTaskButterflyChase(EntityAgent entity, EntityButterfly chaseTarget)
		: base(entity)
	{
		chaseTime = (float)entity.World.Rand.NextDouble() * 7f + 6f;
		targetEntity = chaseTarget;
		targetPos.Set(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		this.taskConfig = taskConfig;
		base.LoadConfig(taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() > 0.03)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (entity.FeetInLiquid)
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
		targetEntity = (EntityButterfly)entity.World.GetNearestEntity(entity.ServerPos.XYZ, seekingRange, seekingRange, delegate(Entity e)
		{
			if (!e.Alive || e.EntityId == entity.EntityId)
			{
				return false;
			}
			return e is EntityButterfly;
		});
		if (targetEntity != null)
		{
			chaseTime = (float)entity.World.Rand.NextDouble() * 7f + 6f;
			targetPos.Set(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
			AiTaskManager taskManager = targetEntity.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
			AiTaskButterflyChase task = taskManager.GetTask<AiTaskButterflyChase>();
			task.targetEntity = entity as EntityButterfly;
			task.targetPos.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
			task.chaseTime = (float)entity.World.Rand.NextDouble() * 7f + 6f;
			taskManager.ExecuteTask<AiTaskButterflyChase>();
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
		if (targetEntity == null)
		{
			return false;
		}
		targetPos.Set(targetEntity.ServerPos.X, targetEntity.ServerPos.Y + (double)(fleeState ? 1 : 0), targetEntity.ServerPos.Z);
		return (chaseTime -= dt) >= 0f;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}
}
