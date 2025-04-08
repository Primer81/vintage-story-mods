using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class AiTaskSeekTargetingEntity : AiTaskSeekEntity
{
	private Entity guardedEntity;

	private Entity lastattackingEntity;

	private long lastattackingEntityFoundMs;

	public AiTaskSeekTargetingEntity(EntityAgent entity)
		: base(entity)
	{
		searchWaitMs = 1000;
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() < 0.1)
		{
			string uid = entity.WatchedAttributes.GetString("guardedPlayerUid");
			if (uid != null)
			{
				guardedEntity = entity.World.PlayerByUid(uid)?.Entity;
			}
			else
			{
				long id = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
				guardedEntity = entity.World.GetEntityById(id);
			}
		}
		if (guardedEntity == null)
		{
			return false;
		}
		if (entity.WatchedAttributes.GetBool("commandSit"))
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - lastattackingEntityFoundMs > 30000)
		{
			lastattackingEntity = null;
		}
		return base.ShouldExecute();
	}

	public override void StartExecute()
	{
		base.StartExecute();
		lastattackingEntityFoundMs = entity.World.ElapsedMilliseconds;
		lastattackingEntity = targetEntity;
	}

	public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
	{
		if (!base.IsTargetableEntity(e, range, ignoreEntityCode))
		{
			return false;
		}
		IAiTask[] tasks = e.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.ActiveTasksBySlot;
		if (e != lastattackingEntity || !e.Alive)
		{
			return tasks?.FirstOrDefault((IAiTask task) => task is AiTaskBaseTargetable aiTaskBaseTargetable && aiTaskBaseTargetable.TargetEntity == guardedEntity && aiTaskBaseTargetable.AggressiveTargeting) != null;
		}
		return true;
	}
}
