using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityBehaviorCreatureCarrier : EntityBehaviorSeatable, IRopeTiedCreatureCarrier
{
	public EntityBehaviorCreatureCarrier(Entity entity)
		: base(entity)
	{
	}
}
