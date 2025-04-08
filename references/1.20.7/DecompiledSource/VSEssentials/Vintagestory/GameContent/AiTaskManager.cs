using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskManager
{
	private Entity entity;

	private List<IAiTask> tasks = new List<IAiTask>();

	private IAiTask[] activeTasksBySlot = new IAiTask[8];

	public bool Shuffle;

	private bool wasRunAiTasks;

	public IAiTask[] ActiveTasksBySlot => activeTasksBySlot;

	public List<IAiTask> AllTasks => tasks;

	public event Action<IAiTask> OnTaskStarted;

	public event Action<IAiTask> OnTaskStopped;

	public event ActionBoolReturn<IAiTask> OnShouldExecuteTask;

	public AiTaskManager(Entity entity)
	{
		this.entity = entity;
	}

	public void AddTask(IAiTask task)
	{
		tasks.Add(task);
		task.ProfilerName = "task-startexecute-" + AiTaskRegistry.TaskCodes[task.GetType()];
	}

	public void RemoveTask(IAiTask task)
	{
		tasks.Remove(task);
	}

	public void AfterInitialize()
	{
		foreach (IAiTask task in tasks)
		{
			task.AfterInitialize();
		}
	}

	public void ExecuteTask(IAiTask task, int slot)
	{
		task.StartExecute();
		activeTasksBySlot[slot] = task;
		if (entity.World.FrameProfiler.Enabled)
		{
			entity.World.FrameProfiler.Mark("task-startexecute-" + AiTaskRegistry.TaskCodes[task.GetType()]);
		}
	}

	public T GetTask<T>() where T : IAiTask
	{
		foreach (IAiTask task in tasks)
		{
			if (task is T)
			{
				return (T)task;
			}
		}
		return default(T);
	}

	public IAiTask GetTask(string id)
	{
		return tasks.FirstOrDefault((IAiTask t) => t.Id == id);
	}

	public void ExecuteTask<T>() where T : IAiTask
	{
		foreach (IAiTask task in tasks)
		{
			if (task is T)
			{
				int slot = task.Slot;
				IAiTask activeTask = activeTasksBySlot[slot];
				if (activeTask != null)
				{
					activeTask.FinishExecute(cancelled: true);
					this.OnTaskStopped?.Invoke(activeTask);
				}
				activeTasksBySlot[slot] = task;
				task.StartExecute();
				this.OnTaskStarted?.Invoke(task);
				entity.World.FrameProfiler.Mark(task.ProfilerName);
			}
		}
	}

	public void StopTask(Type taskType)
	{
		IAiTask[] array = activeTasksBySlot;
		foreach (IAiTask task in array)
		{
			if (task?.GetType() == taskType)
			{
				task.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(task);
				activeTasksBySlot[task.Slot] = null;
			}
		}
		entity.World.FrameProfiler.Mark("finishexecute");
	}

	public void StopTasks()
	{
		IAiTask[] array = activeTasksBySlot;
		foreach (IAiTask task in array)
		{
			if (task != null)
			{
				task.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(task);
				activeTasksBySlot[task.Slot] = null;
			}
		}
	}

	public void OnGameTick(float dt)
	{
		if (!AiRuntimeConfig.RunAiTasks)
		{
			if (wasRunAiTasks)
			{
				IAiTask[] array = activeTasksBySlot;
				for (int l = 0; l < array.Length; l++)
				{
					array[l]?.FinishExecute(cancelled: true);
				}
			}
			wasRunAiTasks = false;
			return;
		}
		wasRunAiTasks = AiRuntimeConfig.RunAiTasks;
		if (Shuffle)
		{
			this.tasks.Shuffle(entity.World.Rand);
		}
		foreach (IAiTask task3 in this.tasks)
		{
			if (task3.Priority < 0f)
			{
				continue;
			}
			int slot = task3.Slot;
			IAiTask oldTask = activeTasksBySlot[slot];
			if ((oldTask == null || task3.Priority > oldTask.PriorityForCancel) && task3.ShouldExecute() && ShouldExecuteTask(task3))
			{
				oldTask?.FinishExecute(cancelled: true);
				if (oldTask != null)
				{
					this.OnTaskStopped?.Invoke(oldTask);
				}
				activeTasksBySlot[slot] = task3;
				task3.StartExecute();
				this.OnTaskStarted?.Invoke(task3);
			}
			if (entity.World.FrameProfiler.Enabled)
			{
				entity.World.FrameProfiler.Mark(task3.ProfilerName);
			}
		}
		for (int j = 0; j < activeTasksBySlot.Length; j++)
		{
			IAiTask task2 = activeTasksBySlot[j];
			if (task2 != null && task2.CanContinueExecute())
			{
				if (!task2.ContinueExecute(dt))
				{
					task2.FinishExecute(cancelled: false);
					this.OnTaskStopped?.Invoke(task2);
					activeTasksBySlot[j] = null;
				}
				if (entity.World.FrameProfiler.Enabled)
				{
					entity.World.FrameProfiler.Mark("task-continueexec-" + AiTaskRegistry.TaskCodes[task2.GetType()]);
				}
			}
		}
		if (!entity.World.EntityDebugMode)
		{
			return;
		}
		string tasks = "";
		int k = 0;
		for (int i = 0; i < activeTasksBySlot.Length; i++)
		{
			IAiTask task = activeTasksBySlot[i];
			if (task != null)
			{
				if (k++ > 0)
				{
					tasks += ", ";
				}
				AiTaskRegistry.TaskCodes.TryGetValue(task.GetType(), out var code);
				tasks = tasks + code + "(p" + task.Priority + ", pc" + task.PriorityForCancel + ")";
			}
		}
		entity.DebugAttributes.SetString("AI Tasks", (tasks.Length > 0) ? tasks : "-");
	}

	private bool ShouldExecuteTask(IAiTask task)
	{
		if (this.OnShouldExecuteTask == null)
		{
			return true;
		}
		bool exec = true;
		Delegate[] invocationList = this.OnShouldExecuteTask.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			ActionBoolReturn<IAiTask> dele = (ActionBoolReturn<IAiTask>)invocationList[i];
			exec &= dele(task);
		}
		return exec;
	}

	public bool IsTaskActive(string id)
	{
		IAiTask[] array = activeTasksBySlot;
		foreach (IAiTask val in array)
		{
			if (val != null && val.Id == id)
			{
				return true;
			}
		}
		return false;
	}

	internal void Notify(string key, object data)
	{
		if (key == "starttask")
		{
			if (activeTasksBySlot.FirstOrDefault((IAiTask t) => t?.Id == (string)data) == null)
			{
				IAiTask task = GetTask((string)data);
				IAiTask activeTask = activeTasksBySlot[task.Slot];
				if (activeTask != null)
				{
					activeTask.FinishExecute(cancelled: true);
					this.OnTaskStopped?.Invoke(activeTask);
				}
				activeTasksBySlot[task.Slot] = null;
				ExecuteTask(task, task.Slot);
			}
			return;
		}
		if (key == "stoptask")
		{
			IAiTask task2 = activeTasksBySlot.FirstOrDefault((IAiTask t) => t?.Id == (string)data);
			if (task2 != null)
			{
				task2.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(task2);
				activeTasksBySlot[task2.Slot] = null;
			}
			return;
		}
		for (int i = 0; i < tasks.Count; i++)
		{
			IAiTask task3 = tasks[i];
			if (!task3.Notify(key, data))
			{
				continue;
			}
			int slot = tasks[i].Slot;
			if (activeTasksBySlot[slot] == null || task3.Priority > activeTasksBySlot[slot].PriorityForCancel)
			{
				if (activeTasksBySlot[slot] != null)
				{
					activeTasksBySlot[slot].FinishExecute(cancelled: true);
					this.OnTaskStopped?.Invoke(activeTasksBySlot[slot]);
				}
				activeTasksBySlot[slot] = task3;
				task3.StartExecute();
				this.OnTaskStarted?.Invoke(task3);
			}
		}
	}

	internal void OnStateChanged(EnumEntityState beforeState)
	{
		foreach (IAiTask task in tasks)
		{
			task.OnStateChanged(beforeState);
		}
	}

	internal void OnEntitySpawn()
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntitySpawn();
		}
	}

	internal void OnEntityLoaded()
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntityLoaded();
		}
	}

	internal void OnEntityDespawn(EntityDespawnData reason)
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntityDespawn(reason);
		}
	}

	internal void OnEntityHurt(DamageSource source, float damage)
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntityHurt(source, damage);
		}
	}
}
