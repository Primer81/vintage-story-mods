using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Vintagestory.API.Common;

public abstract class AiGoalBase
{
	public Random rand;

	public EntityAgent entity;

	public IWorldAccessor world;

	protected float priority;

	protected float priorityForCancel;

	protected int mincooldown;

	protected int maxcooldown;

	protected double mincooldownHours;

	protected double maxcooldownHours;

	protected long cooldownUntilMs;

	protected double cooldownUntilTotalHours;

	protected PathTraverserBase pathTraverser;

	private Queue<AiActionBase> activeActions = new Queue<AiActionBase>();

	public virtual float Priority => priority;

	public virtual float PriorityForCancel => priorityForCancel;

	public AiGoalBase(EntityAgent entity)
	{
		this.entity = entity;
		world = entity.World;
		rand = new Random((int)entity.EntityId);
		pathTraverser = entity.GetBehavior<EntityBehaviorGoalAI>().PathTraverser;
	}

	public virtual void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		priority = taskConfig["priority"].AsFloat();
		priorityForCancel = taskConfig["priorityForCancel"].AsFloat(priority);
		mincooldown = (taskConfig["mincooldown"]?.AsInt()).Value;
		maxcooldown = (taskConfig["maxcooldown"]?.AsInt(100)).Value;
		mincooldownHours = (taskConfig["mincooldownHours"]?.AsDouble()).Value;
		maxcooldownHours = (taskConfig["maxcooldownHours"]?.AsDouble()).Value;
		int initialmincooldown = (taskConfig["initialMinCoolDown"]?.AsInt(mincooldown)).Value;
		int initialmaxcooldown = (taskConfig["initialMaxCoolDown"]?.AsInt(maxcooldown)).Value;
		cooldownUntilMs = entity.World.ElapsedMilliseconds + initialmincooldown + entity.World.Rand.Next(initialmaxcooldown - initialmincooldown);
	}

	public virtual bool ShouldExecuteAll()
	{
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours)
		{
			return false;
		}
		return ShouldExecute();
	}

	protected abstract bool ShouldExecute();

	public virtual void StartExecuteAll()
	{
		StartExecute();
	}

	protected virtual void StartExecute()
	{
	}

	public virtual bool ContinueExecuteAll(float dt)
	{
		return ContinueExecute(dt);
	}

	protected abstract bool ContinueExecute(float dt);

	public virtual void FinishExecuteAll(bool cancelled)
	{
		cooldownUntilMs = entity.World.ElapsedMilliseconds + mincooldown + entity.World.Rand.Next(maxcooldown - mincooldown);
		cooldownUntilTotalHours = entity.World.Calendar.TotalHours + mincooldownHours + entity.World.Rand.NextDouble() * (maxcooldownHours - mincooldownHours);
		FinishExecute(cancelled);
	}

	protected virtual void FinishExecute(bool cancelled)
	{
	}

	public virtual void OnStateChanged(EnumEntityState beforeState)
	{
		if (entity.State == EnumEntityState.Active)
		{
			cooldownUntilMs = entity.World.ElapsedMilliseconds + mincooldown + entity.World.Rand.Next(maxcooldown - mincooldown);
		}
	}

	public virtual bool Notify(string key, object data)
	{
		return false;
	}
}
