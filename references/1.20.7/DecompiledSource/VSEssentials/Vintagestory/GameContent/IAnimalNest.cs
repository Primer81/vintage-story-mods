using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface IAnimalNest : IPointOfInterest
{
	float DistanceWeighting { get; }

	bool IsSuitableFor(Entity entity);

	bool Occupied(Entity entity);

	void SetOccupier(Entity entity);

	bool TryAddEgg(Entity entity, string chickCode, double incubationTime);
}
