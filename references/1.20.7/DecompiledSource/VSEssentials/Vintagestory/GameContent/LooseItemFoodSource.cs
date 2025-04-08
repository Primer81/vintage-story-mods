using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class LooseItemFoodSource : IAnimalFoodSource, IPointOfInterest
{
	private EntityItem entity;

	public ItemStack ItemStack => entity.Itemstack;

	public Vec3d Position => entity.ServerPos.XYZ;

	public string Type => "food";

	public LooseItemFoodSource(EntityItem entity)
	{
		this.entity = entity;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		this.entity.Itemstack.StackSize--;
		if (this.entity.Itemstack.StackSize <= 0)
		{
			this.entity.Die();
		}
		if (this.entity.Itemstack.StackSize < 0)
		{
			return 0f;
		}
		return 1f;
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		return true;
	}
}
