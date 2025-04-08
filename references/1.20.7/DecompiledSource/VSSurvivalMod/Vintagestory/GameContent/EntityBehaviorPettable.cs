using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorPettable : EntityBehavior, IPettable
{
	private long lastPetTotalMs;

	private float petDurationS;

	public EntityBehaviorPettable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		if (entity.World.Side == EnumAppSide.Server && entity.WatchedAttributes.GetInt("generation") >= attributes["minGeneration"].AsInt(1))
		{
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.OnShouldExecuteTask += TaskManager_OnShouldExecuteTask;
		}
	}

	private bool TaskManager_OnShouldExecuteTask(IAiTask task)
	{
		if ((double)petDurationS >= 0.6)
		{
			return false;
		}
		return true;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
		if (byEntity is EntityPlayer && byEntity.Controls.RightMouseDown && byEntity.RightHandItemSlot.Empty && byEntity.Pos.DistanceTo(entity.Pos) < 1.2)
		{
			if (entity.World.ElapsedMilliseconds - lastPetTotalMs < 500)
			{
				petDurationS += (float)(entity.World.ElapsedMilliseconds - lastPetTotalMs) / 1000f;
			}
			else
			{
				petDurationS = 0f;
			}
			lastPetTotalMs = entity.World.ElapsedMilliseconds;
			if ((double)petDurationS >= 0.6 && entity.World.Side == EnumAppSide.Server)
			{
				AiTaskManager taskManager = entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
				taskManager.StopTask(typeof(AiTaskWander));
				taskManager.StopTask(typeof(AiTaskSeekEntity));
				taskManager.StopTask(typeof(AiTaskGotoEntity));
			}
		}
		else
		{
			petDurationS = 0f;
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.ElapsedMilliseconds - lastPetTotalMs > 400)
		{
			petDurationS = 0f;
		}
		base.OnGameTick(deltaTime);
	}

	public override string PropertyName()
	{
		return "pettable";
	}

	public bool CanPet(Entity byEntity)
	{
		return true;
	}
}
