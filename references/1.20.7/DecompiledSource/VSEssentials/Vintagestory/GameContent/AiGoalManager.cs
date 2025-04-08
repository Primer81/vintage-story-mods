using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class AiGoalManager
{
	private Entity entity;

	private List<AiGoalBase> Goals = new List<AiGoalBase>();

	private AiGoalBase activeGoal;

	public AiGoalManager(Entity entity)
	{
		this.entity = entity;
	}

	public void AddGoal(AiGoalBase goal)
	{
		Goals.Add(goal);
	}

	public void RemoveGoal(AiGoalBase goal)
	{
		Goals.Remove(goal);
	}

	public void OnGameTick(float dt)
	{
		foreach (AiGoalBase newGoal in Goals)
		{
			if ((activeGoal == null || newGoal.Priority > activeGoal.PriorityForCancel) && newGoal.ShouldExecuteAll())
			{
				activeGoal?.FinishExecuteAll(cancelled: true);
				activeGoal = newGoal;
				newGoal.StartExecuteAll();
			}
		}
		if (activeGoal != null && !activeGoal.ContinueExecuteAll(dt))
		{
			activeGoal.FinishExecuteAll(cancelled: false);
			activeGoal = null;
		}
		if (entity.World.EntityDebugMode)
		{
			string tasks = "";
			if (activeGoal != null)
			{
				tasks = tasks + AiTaskRegistry.TaskCodes[activeGoal.GetType()] + "(" + activeGoal.Priority + ")";
			}
			entity.DebugAttributes.SetString("AI Goal", (tasks.Length > 0) ? tasks : "-");
		}
	}

	internal void Notify(string key, object data)
	{
		for (int i = 0; i < Goals.Count; i++)
		{
			AiGoalBase newGoal = Goals[i];
			if (newGoal.Notify(key, data) && (newGoal == null || newGoal.Priority > activeGoal.PriorityForCancel))
			{
				activeGoal?.FinishExecuteAll(cancelled: true);
				activeGoal = newGoal;
				newGoal.StartExecuteAll();
			}
		}
	}

	internal void OnStateChanged(EnumEntityState beforeState)
	{
		foreach (IAiTask goal in Goals)
		{
			goal.OnStateChanged(beforeState);
		}
	}
}
