using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerWobbledForest : MapLayerBase
{
	private NormalizedSimplexNoise noisegen;

	private float multiplier;

	private int offset;

	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenY;

	public float wobbleIntensity;

	public MapLayerWobbledForest(long seed, int octaves, float persistence, float scale, float multiplier = 255f, int offset = 0)
		: base(seed)
	{
		double[] frequencies = new double[3];
		double[] amplitudes = new double[3];
		for (int i = 0; i < octaves; i++)
		{
			frequencies[i] = Math.Pow(3.0, i) * 1.0 / (double)scale;
			amplitudes[i] = Math.Pow(persistence, i);
		}
		noisegen = new NormalizedSimplexNoise(amplitudes, frequencies, seed);
		this.offset = offset;
		this.multiplier = multiplier;
		int woctaves = 3;
		float wscale = 128f;
		float wpersistence = 0.9f;
		wobbleIntensity = scale / 3f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1f / wscale, wpersistence, seed + 2);
		noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1f / wscale, wpersistence, seed + 1231296);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] outData = new int[sizeX * sizeZ];
		for (int z = 0; z < sizeZ; z++)
		{
			for (int x = 0; x < sizeX; x++)
			{
				int offsetX = (int)((double)wobbleIntensity * noisegenX.Noise(xCoord + x, zCoord + z));
				int offsetY = (int)((double)wobbleIntensity * noisegenY.Noise(xCoord + x, zCoord + z));
				int wobbledX = xCoord + x + offsetX;
				int wobbledZ = zCoord + z + offsetY;
				double forestValue = (double)offset + (double)multiplier * noisegen.Noise(wobbledX, wobbledZ);
				int unpaddedInt = inputMap.GetUnpaddedInt(x * inputMap.InnerSize / outputMap.InnerSize, z * inputMap.InnerSize / outputMap.InnerSize);
				float rain = (unpaddedInt >> 8) & 0xFF;
				float temperature = (unpaddedInt >> 16) & 0xFF;
				outData[z * sizeX + x] = (int)GameMath.Clamp(forestValue - (double)GameMath.Clamp(128f - rain * temperature / 65025f, 0f, 128f), 0.0, 255.0);
			}
		}
		return outData;
	}
}
