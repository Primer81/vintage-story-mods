namespace Vintagestory.API.MathTools;

public class FluidBlockPos : BlockPos
{
	public FluidBlockPos()
	{
	}

	public FluidBlockPos(int x, int y, int z, int dim)
	{
		X = x;
		Y = y;
		Z = z;
		dimension = dim;
	}

	public override BlockPos Copy()
	{
		return new FluidBlockPos(X, Y, Z, dimension);
	}
}
