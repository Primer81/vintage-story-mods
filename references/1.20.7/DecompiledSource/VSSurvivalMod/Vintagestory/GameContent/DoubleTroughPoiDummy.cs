using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class DoubleTroughPoiDummy : IAnimalFoodSource, IPointOfInterest
{
	private BlockEntityTrough be;

	public Vec3d Position { get; set; }

	public string Type => be.Type;

	public DoubleTroughPoiDummy(BlockEntityTrough be)
	{
		this.be = be;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		return be.ConsumeOnePortion(entity);
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		return be.IsSuitableFor(entity, diet);
	}
}
