using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class AiTaskStayCloseToGuardedEntity : AiTaskStayCloseToEntity
{
	public Entity guardedEntity;

	public AiTaskStayCloseToGuardedEntity(EntityAgent entity)
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
		if (base.rand.NextDouble() > 0.10000000149011612)
		{
			return false;
		}
		if (entity.WatchedAttributes.GetBool("commandSit"))
		{
			return false;
		}
		targetEntity = guardedEntity;
		double x = guardedEntity.ServerPos.X;
		double y = guardedEntity.ServerPos.Y;
		double z = guardedEntity.ServerPos.Z;
		return (double)entity.ServerPos.SquareDistanceTo(x, y, z) > (double)(maxDistance * maxDistance);
	}

	public override void StartExecute()
	{
		base.StartExecute();
		float size = targetEntity.SelectionBox.XSize;
		pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, moveSpeed, size + 0.2f, base.OnGoalReached, base.OnStuck, base.tryTeleport, 1000, 1);
		targetOffset.Set(entity.World.Rand.NextDouble() * 2.0 - 1.0, 0.0, entity.World.Rand.NextDouble() * 2.0 - 1.0);
		stuck = false;
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public Entity GetGuardedEntity()
	{
		string uid = entity.WatchedAttributes.GetString("guardedPlayerUid");
		if (uid != null)
		{
			return entity.World.PlayerByUid(uid)?.Entity;
		}
		long id = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
		return entity.World.GetEntityById(id);
	}
}
