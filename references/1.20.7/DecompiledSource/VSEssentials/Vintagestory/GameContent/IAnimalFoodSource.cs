using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface IAnimalFoodSource : IPointOfInterest
{
	bool IsSuitableFor(Entity entity, CreatureDiet diet);

	float ConsumeOnePortion(Entity entity);
}
