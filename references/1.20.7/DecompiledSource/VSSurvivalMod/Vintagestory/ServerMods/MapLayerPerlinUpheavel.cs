using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class MapLayerPerlinUpheavel : MapLayerBase
{
	private NormalizedSimplexNoise superLowResNoiseGen;

	private float multiplier;

	private int offset;

	public float noiseOffset;

	public MapLayerPerlinUpheavel(long seed, float noiseOffset, float scale, float multiplier = 255f, int offset = 0)
		: base(seed)
	{
		superLowResNoiseGen = new NormalizedSimplexNoise(new double[4] { 1.0, 0.5, 0.25, 0.15 }, new double[4]
		{
			0.5 / (double)scale,
			1f / scale,
			2f / scale,
			4f / scale
		}, seed + 1685);
		this.noiseOffset = 1f - noiseOffset;
		this.offset = offset;
		this.multiplier = multiplier;
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] outData = new int[sizeX * sizeZ];
		for (int z = 0; z < sizeZ; z++)
		{
			for (int x = 0; x < sizeX; x++)
			{
				double lowresvalue = GameMath.Clamp((superLowResNoiseGen.Noise(xCoord + x, zCoord + z) - (double)noiseOffset) * 15.0, 0.0, 1.0);
				double outvalue = (float)offset + multiplier;
				outData[z * sizeX + x] = (int)GameMath.Clamp(lowresvalue * outvalue, 0.0, 255.0);
			}
		}
		return outData;
	}
}
