using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

public class LandformVariant : WorldPropertyVariant
{
	[JsonIgnore]
	public int index;

	[JsonIgnore]
	public float[] TerrainYThresholds;

	[JsonIgnore]
	public int ColorInt;

	[JsonIgnore]
	public double WeightTmp;

	[JsonProperty]
	public string HexColor;

	[JsonProperty]
	public double Weight;

	[JsonProperty]
	public bool UseClimateMap;

	[JsonProperty]
	public float MinTemp = -50f;

	[JsonProperty]
	public float MaxTemp = 50f;

	[JsonProperty]
	public int MinRain;

	[JsonProperty]
	public int MaxRain = 255;

	[JsonProperty]
	public bool UseWindMap;

	[JsonProperty]
	public int MinWindStrength;

	[JsonProperty]
	public int MaxWindStrength;

	[JsonProperty]
	public double[] TerrainOctaves;

	[JsonProperty]
	public double[] TerrainOctaveThresholds = new double[11];

	[JsonProperty]
	public float[] TerrainYKeyPositions;

	[JsonProperty]
	public float[] TerrainYKeyThresholds;

	[JsonProperty]
	public LandformVariant[] Mutations = new LandformVariant[0];

	[JsonProperty]
	public float Chance;

	private static Random rnd = new Random();

	public void Init(IWorldManagerAPI api, int index)
	{
		this.index = index;
		expandOctaves(api);
		LerpThresholds(api.MapSizeY);
		ColorInt = rnd.Next(int.MaxValue) | -16777216;
	}

	protected void expandOctaves(IWorldManagerAPI api)
	{
		int octaves = TerraGenConfig.GetTerrainOctaveCount(api.MapSizeY);
		int l = octaves - TerrainOctaves.Length;
		if (l > 0)
		{
			double[] ext = new double[l].Fill(TerrainOctaves[TerrainOctaves.Length - 1]);
			double addSum = 0.0;
			for (int k = 0; k < ext.Length; k++)
			{
				double val = Math.Pow(0.8, k + 1);
				ext[k] *= val;
				addSum += val;
			}
			double prevSum = 0.0;
			for (int j = 0; j < TerrainOctaves.Length; j++)
			{
				prevSum += TerrainOctaves[j];
			}
			for (int i = 0; i < TerrainOctaves.Length; i++)
			{
				TerrainOctaves[i] *= (prevSum + addSum) / prevSum;
			}
			TerrainOctaves = TerrainOctaves.Append(ext);
		}
		int m = octaves - TerrainOctaveThresholds.Length;
		if (m > 0)
		{
			TerrainOctaveThresholds = TerrainOctaveThresholds.Append(new double[m].Fill(TerrainOctaveThresholds[TerrainOctaveThresholds.Length - 1]));
		}
	}

	private void LerpThresholds(int mapSizeY)
	{
		TerrainYThresholds = new float[mapSizeY];
		float curThreshold = 1f;
		float curThresholdY = 0f;
		int curThresholdPos = -1;
		for (int y = 0; y < mapSizeY; y++)
		{
			if (curThresholdPos + 1 >= TerrainYKeyThresholds.Length)
			{
				TerrainYThresholds[y] = 1f;
				continue;
			}
			if ((float)y >= TerrainYKeyPositions[curThresholdPos + 1] * (float)mapSizeY)
			{
				curThreshold = TerrainYKeyThresholds[curThresholdPos + 1];
				curThresholdY = TerrainYKeyPositions[curThresholdPos + 1] * (float)mapSizeY;
				curThresholdPos++;
			}
			float nextThreshold = 0f;
			float nextThresholdY = mapSizeY;
			if (curThresholdPos + 1 < TerrainYKeyThresholds.Length)
			{
				nextThreshold = TerrainYKeyThresholds[curThresholdPos + 1];
				nextThresholdY = TerrainYKeyPositions[curThresholdPos + 1] * (float)mapSizeY;
			}
			float range = nextThresholdY - curThresholdY;
			float distance = ((float)y - curThresholdY) / range;
			if (range == 0f)
			{
				string pos = "";
				for (int i = 0; i < TerrainYKeyPositions.Length; i++)
				{
					if (i > 0)
					{
						pos += ", ";
					}
					pos += TerrainYKeyPositions[i] * (float)mapSizeY;
				}
				throw new Exception(string.Concat("Illegal TerrainYKeyPositions in landforms.js, Landform ", Code, ", key positions must be more than 0 blocks apart. Translated key positions for this maps world height: ", pos));
			}
			TerrainYThresholds[y] = 1f - GameMath.Lerp(curThreshold, nextThreshold, distance);
		}
	}

	public float[] AddTerrainNoiseThresholds(float[] thresholds, float weight)
	{
		for (int y = 0; y < thresholds.Length; y++)
		{
			thresholds[y] += weight * TerrainYThresholds[y];
		}
		return thresholds;
	}
}
