using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerCustomPerlin : MapLayerBase
{
	private SimplexNoise noisegen;

	private double[] thresholds;

	public int clampMin;

	public int clampMax = 255;

	public MapLayerCustomPerlin(long seed, double[] amplitudes, double[] frequencies, double[] thresholds)
		: base(seed)
	{
		noisegen = new SimplexNoise(amplitudes, frequencies, seed + 12321);
		this.thresholds = thresholds;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] outData = new int[sizeX * sizeZ];
		for (int z = 0; z < sizeZ; z++)
		{
			for (int x = 0; x < sizeX; x++)
			{
				outData[z * sizeX + x] = (int)GameMath.Clamp(noisegen.Noise(xCoord + x, zCoord + z, thresholds), clampMin, clampMax);
			}
		}
		return outData;
	}
}
