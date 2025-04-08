using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

internal class NoiseLandforms : NoiseBase
{
	public static LandformsWorldProperty landforms;

	public float scale;

	public NoiseLandforms(long seed, ICoreServerAPI api, float scale)
		: base(seed)
	{
		LoadLandforms(api);
		this.scale = scale;
	}

	public static void LoadLandforms(ICoreServerAPI api)
	{
		landforms = api.Assets.Get("worldgen/landforms.json").ToObject<LandformsWorldProperty>();
		int quantityMutations = 0;
		for (int k = 0; k < landforms.Variants.Length; k++)
		{
			LandformVariant variant2 = landforms.Variants[k];
			variant2.index = k;
			variant2.Init(api.WorldManager, k);
			if (variant2.Mutations != null)
			{
				quantityMutations += variant2.Mutations.Length;
			}
		}
		landforms.LandFormsByIndex = new LandformVariant[quantityMutations + landforms.Variants.Length];
		for (int j = 0; j < landforms.Variants.Length; j++)
		{
			landforms.LandFormsByIndex[j] = landforms.Variants[j];
		}
		int nextIndex = landforms.Variants.Length;
		for (int i = 0; i < landforms.Variants.Length; i++)
		{
			LandformVariant variant = landforms.Variants[i];
			if (variant.Mutations == null)
			{
				continue;
			}
			for (int l = 0; l < variant.Mutations.Length; l++)
			{
				LandformVariant variantMut = variant.Mutations[l];
				if (variantMut.TerrainOctaves == null)
				{
					variantMut.TerrainOctaves = variant.TerrainOctaves;
				}
				if (variantMut.TerrainOctaveThresholds == null)
				{
					variantMut.TerrainOctaveThresholds = variant.TerrainOctaveThresholds;
				}
				if (variantMut.TerrainYKeyPositions == null)
				{
					variantMut.TerrainYKeyPositions = variant.TerrainYKeyPositions;
				}
				if (variantMut.TerrainYKeyThresholds == null)
				{
					variantMut.TerrainYKeyThresholds = variant.TerrainYKeyThresholds;
				}
				landforms.LandFormsByIndex[nextIndex] = variantMut;
				variantMut.Init(api.WorldManager, nextIndex);
				nextIndex++;
			}
		}
	}

	public int GetLandformIndexAt(int unscaledXpos, int unscaledZpos, int temp, int rain)
	{
		float xpos = (float)unscaledXpos / scale;
		float num = (float)unscaledZpos / scale;
		int xposInt = (int)xpos;
		int zposInt = (int)num;
		int parentIndex = GetParentLandformIndexAt(xposInt, zposInt, temp, rain);
		LandformVariant[] mutations = landforms.Variants[parentIndex].Mutations;
		if (mutations != null && mutations.Length != 0)
		{
			InitPositionSeed(unscaledXpos / 2, unscaledZpos / 2);
			float chance = (float)NextInt(101) / 100f;
			for (int i = 0; i < mutations.Length; i++)
			{
				LandformVariant variantMut = mutations[i];
				if (variantMut.UseClimateMap)
				{
					int num2 = rain - GameMath.Clamp(rain, variantMut.MinRain, variantMut.MaxRain);
					double distTemp = (float)temp - GameMath.Clamp(temp, variantMut.MinTemp, variantMut.MaxTemp);
					if (num2 != 0 || distTemp != 0.0)
					{
						continue;
					}
				}
				chance -= mutations[i].Chance;
				if (chance <= 0f)
				{
					return mutations[i].index;
				}
			}
		}
		return parentIndex;
	}

	public int GetParentLandformIndexAt(int xpos, int zpos, int temp, int rain)
	{
		InitPositionSeed(xpos, zpos);
		double weightSum = 0.0;
		int i;
		for (i = 0; i < landforms.Variants.Length; i++)
		{
			double weight = landforms.Variants[i].Weight;
			if (landforms.Variants[i].UseClimateMap)
			{
				int num = rain - GameMath.Clamp(rain, landforms.Variants[i].MinRain, landforms.Variants[i].MaxRain);
				double distTemp = (float)temp - GameMath.Clamp(temp, landforms.Variants[i].MinTemp, landforms.Variants[i].MaxTemp);
				if (num != 0 || distTemp != 0.0)
				{
					weight = 0.0;
				}
			}
			landforms.Variants[i].WeightTmp = weight;
			weightSum += weight;
		}
		double rand = weightSum * (double)NextInt(10000) / 10000.0;
		for (i = 0; i < landforms.Variants.Length; i++)
		{
			rand -= landforms.Variants[i].WeightTmp;
			if (rand <= 0.0)
			{
				return landforms.Variants[i].index;
			}
		}
		return landforms.Variants[i].index;
	}
}
