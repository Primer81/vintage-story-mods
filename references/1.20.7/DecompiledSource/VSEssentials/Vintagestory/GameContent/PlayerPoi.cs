using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class PlayerPoi : IAnimalFoodSource, IPointOfInterest
{
	private EntityPlayer plr;

	private Vec3d pos = new Vec3d();

	public Vec3d Position
	{
		get
		{
			pos.Set(plr.Pos.X, plr.Pos.Y, plr.Pos.Z);
			return pos;
		}
	}

	public string Type => "food";

	public PlayerPoi(EntityPlayer plr)
	{
		this.plr = plr;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		return 0f;
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		return false;
	}
}
