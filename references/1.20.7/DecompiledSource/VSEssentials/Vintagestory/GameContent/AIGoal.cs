using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class AIGoal
{
	private IWorldAccessor world;

	private BehaviorGoapAI behavior;

	public AIGoal(BehaviorGoapAI behavior, IWorldAccessor world)
	{
		this.world = world;
		this.behavior = behavior;
	}

	public virtual bool ShouldExecute()
	{
		return false;
	}
}
