using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class AiTaskMeleeAttackTargetingEntity : AiTaskMeleeAttack
{
	private Entity guardedEntity;

	private Entity lastattackingEntity;

	private long lastattackingEntityFoundMs;

	public AiTaskMeleeAttackTargetingEntity(EntityAgent entity)
		: base(entity)
	{
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() < 0.1)
		{
			guardedEntity = GetGuardedEntity();
		}
		if (guardedEntity == null)
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - lastattackingEntityFoundMs > 30000)
		{
			lastattackingEntity = null;
		}
		if (attackedByEntity == guardedEntity)
		{
			attackedByEntity = null;
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
		if (e == guardedEntity)
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
