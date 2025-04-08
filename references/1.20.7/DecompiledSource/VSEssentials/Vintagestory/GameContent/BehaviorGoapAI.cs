using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BehaviorGoapAI : EntityBehavior
{
	private ITreeAttribute goaltree;

	internal float Aggressivness => goaltree.GetFloat("aggressivness");

	public BehaviorGoapAI(Entity entity)
		: base(entity)
	{
		goaltree = entity.WatchedAttributes.GetTreeAttribute("goaltree");
	}

	public override string PropertyName()
	{
		return "goaloriented";
	}
}
