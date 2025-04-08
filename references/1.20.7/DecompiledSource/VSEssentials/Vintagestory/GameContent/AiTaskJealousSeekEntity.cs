using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class AiTaskJealousSeekEntity : AiTaskSeekEntity
{
	private Entity guardedEntity;

	public AiTaskJealousSeekEntity(EntityAgent entity)
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
		return base.ShouldExecute();
	}

	public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
	{
		if (!base.IsTargetableEntity(e, range, ignoreEntityCode))
		{
			return false;
		}
		return e.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.AllTasks?.FirstOrDefault((IAiTask task) => task is AiTaskStayCloseToGuardedEntity aiTaskStayCloseToGuardedEntity && aiTaskStayCloseToGuardedEntity.guardedEntity == guardedEntity) != null;
	}
}
