namespace Vintagestory.ServerMods;

public abstract class NoiseClimate : NoiseBase
{
	public float tempMul = 1f;

	public float rainMul = 1f;

	public NoiseClimate(long worldSeed)
		: base(worldSeed)
	{
	}

	public abstract int GetClimateAt(int posX, int posZ);

	public abstract int GetLerpedClimateAt(double posX, double posZ);

	public abstract int GetLerpedClimateAt(double posX, double posZ, int[] climateCache, int sizeX);
}
