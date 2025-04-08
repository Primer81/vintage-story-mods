using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityBehaviorTaskAI : EntityBehavior
{
	public AiTaskManager TaskManager;

	public WaypointsTraverser PathTraverser;

	public EntityBehaviorTaskAI(Entity entity)
		: base(entity)
	{
		TaskManager = new AiTaskManager(entity);
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		TaskManager.OnEntitySpawn();
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		TaskManager.OnEntityLoaded();
	}

	public override void OnEntityDespawn(EntityDespawnData reason)
	{
		base.OnEntityDespawn(reason);
		TaskManager.OnEntityDespawn(reason);
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		base.OnEntityReceiveDamage(damageSource, ref damage);
		TaskManager.OnEntityHurt(damageSource, damage);
	}

	public override void Initialize(EntityProperties properties, JsonObject aiconfig)
	{
		if (!(entity is EntityAgent))
		{
			entity.World.Logger.Error("The task ai currently only works on entities inheriting from EntityAgent. Will ignore loading tasks for entity {0} ", entity.Code);
			return;
		}
		TaskManager.Shuffle = aiconfig["shuffle"].AsBool();
		EnumAICreatureType ect = EnumAICreatureType.Default;
		string typestr = aiconfig["aiCreatureType"].AsString("Default");
		if (!Enum.TryParse<EnumAICreatureType>(typestr, out ect))
		{
			ect = EnumAICreatureType.Default;
			entity.World.Logger.Warning("Entity {0} Task AI, invalid aiCreatureType {1}. Will default to 'Default'", entity.Code, typestr);
		}
		PathTraverser = new WaypointsTraverser(entity as EntityAgent, ect);
		JsonObject[] tasks = aiconfig["aitasks"]?.AsArray();
		if (tasks == null)
		{
			return;
		}
		JsonObject[] array = tasks;
		foreach (JsonObject taskConfig in array)
		{
			string taskCode = taskConfig["code"]?.AsString();
			if (!taskConfig["enabled"].AsBool(defaultValue: true))
			{
				continue;
			}
			Type taskType = null;
			if (!AiTaskRegistry.TaskTypes.TryGetValue(taskCode, out taskType))
			{
				entity.World.Logger.Error("Task with code {0} for entity {1} does not exist. Ignoring.", taskCode, entity.Code);
				continue;
			}
			IAiTask task = (IAiTask)Activator.CreateInstance(taskType, (EntityAgent)entity);
			try
			{
				task.LoadConfig(taskConfig, aiconfig);
			}
			catch (Exception)
			{
				entity.World.Logger.Error("Task with code {0} for entity {1}: Unable to load json code.", taskCode, entity.Code);
				throw;
			}
			TaskManager.AddTask(task);
		}
	}

	public override void AfterInitialized(bool onSpawn)
	{
		TaskManager.AfterInitialize();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State == EnumEntityState.Active && entity.Alive)
		{
			entity.World.FrameProfiler.Mark("ai-init");
			PathTraverser.OnGameTick(deltaTime);
			entity.World.FrameProfiler.Mark("ai-pathfinding");
			entity.World.FrameProfiler.Enter("ai-tasks");
			TaskManager.OnGameTick(deltaTime);
			entity.World.FrameProfiler.Leave();
		}
	}

	public override void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handled)
	{
		TaskManager.OnStateChanged(beforeState);
	}

	public override void Notify(string key, object data)
	{
		TaskManager.Notify(key, data);
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		base.GetInfoText(infotext);
	}

	public override string PropertyName()
	{
		return "taskai";
	}
}
