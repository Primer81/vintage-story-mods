using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerPerlin : MapLayerBase
{
	private NormalizedSimplexNoise noisegen;

	private float multiplier;

	private double[] thresholds;

	public MapLayerPerlin(long seed, int octaves, float persistence, int scale, int multiplier)
		: base(seed)
	{
		noisegen = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / (float)scale, persistence, seed + 12321);
		this.multiplier = multiplier;
	}

	public MapLayerPerlin(long seed, int octaves, float persistence, int scale, int multiplier, double[] thresholds)
		: base(seed)
	{
		noisegen = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / (float)scale, persistence, seed + 12321);
		this.multiplier = multiplier;
		this.thresholds = thresholds;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] outData = new int[sizeX * sizeZ];
		if (thresholds != null)
		{
			for (int z = 0; z < sizeZ; z++)
			{
				for (int x = 0; x < sizeX; x++)
				{
					outData[z * sizeX + x] = (int)GameMath.Clamp((double)multiplier * noisegen.Noise(xCoord + x, zCoord + z, thresholds), 0.0, 255.0);
				}
			}
		}
		else
		{
			for (int z2 = 0; z2 < sizeZ; z2++)
			{
				for (int x2 = 0; x2 < sizeX; x2++)
				{
					outData[z2 * sizeX + x2] = (int)GameMath.Clamp((double)multiplier * noisegen.Noise(xCoord + x2, zCoord + z2), 0.0, 255.0);
				}
			}
		}
		return outData;
	}

	public int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ, double[] thresholds)
	{
		int[] outData = new int[sizeX * sizeZ];
		for (int z = 0; z < sizeZ; z++)
		{
			for (int x = 0; x < sizeX; x++)
			{
				outData[z * sizeX + x] = (int)GameMath.Clamp((double)multiplier * noisegen.Noise(xCoord + x, zCoord + z, thresholds), 0.0, 255.0);
			}
		}
		return outData;
	}
}
