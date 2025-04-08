using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class WgenTreeSupplier
{
	private ICoreServerAPI api;

	internal TreeGenProperties treeGenProps;

	internal TreeGeneratorsUtil treeGenerators;

	private float worldheight;

	private Dictionary<TreeVariant, float> distances = new Dictionary<TreeVariant, float>();

	public WgenTreeSupplier(ICoreServerAPI api)
	{
		treeGenerators = new TreeGeneratorsUtil(api);
		this.api = api;
	}

	internal void LoadTrees()
	{
		treeGenProps = api.Assets.Get("worldgen/treengenproperties.json").ToObject<TreeGenProperties>();
		treeGenProps.descVineMinTempRel = (float)Climate.DescaleTemperature(treeGenProps.vinesMinTemp) / 255f;
		treeGenerators.LoadTreeGenerators();
		worldheight = api.WorldManager.MapSizeY;
	}

	public TreeGenInstance GetRandomTreeGenForClimate(LCGRandom rnd, int climate, int forest, int y, bool isUnderwater)
	{
		return GetRandomGenForClimate(rnd, treeGenProps.TreeGens, climate, forest, y, isUnderwater);
	}

	public TreeGenInstance GetRandomShrubGenForClimate(LCGRandom rnd, int climate, int forest, int y)
	{
		return GetRandomGenForClimate(rnd, treeGenProps.ShrubGens, climate, forest, y, isUnderwater: false);
	}

	public TreeGenInstance GetRandomGenForClimate(LCGRandom rnd, TreeVariant[] gens, int climate, int forest, int y, bool isUnderwater)
	{
		int rain = Climate.GetRainFall((climate >> 8) & 0xFF, y);
		int temp = Climate.GetScaledAdjustedTemperature((climate >> 16) & 0xFF, y - TerraGenConfig.seaLevel);
		float heightRel = ((float)y - (float)TerraGenConfig.seaLevel) / ((float)api.WorldManager.MapSizeY - (float)TerraGenConfig.seaLevel);
		int fertility = Climate.GetFertility(rain, temp, heightRel);
		float total = 0f;
		distances.Clear();
		foreach (TreeVariant variant in gens)
		{
			if ((!isUnderwater || variant.Habitat != 0) && (isUnderwater || variant.Habitat != EnumTreeHabitat.Water))
			{
				float fertDist = (float)Math.Abs(fertility - variant.FertMid) / variant.FertRange;
				float rainDist = (float)Math.Abs(rain - variant.RainMid) / variant.RainRange;
				float tempDist = (float)Math.Abs(temp - variant.TempMid) / variant.TempRange;
				float forestDist = (float)Math.Abs(forest - variant.ForestMid) / variant.ForestRange;
				float heightDist = Math.Abs((float)y / worldheight - variant.HeightMid) / variant.HeightRange;
				double distSq = Math.Max(0f, 1.2f * fertDist * fertDist - 1f) + Math.Max(0f, 1.2f * rainDist * rainDist - 1f) + Math.Max(0f, 1.2f * tempDist * tempDist - 1f) + Math.Max(0f, 1.2f * forestDist * forestDist - 1f) + Math.Max(0f, 1.2f * heightDist * heightDist - 1f);
				if (!(rnd.NextDouble() < distSq))
				{
					float distance = GameMath.Clamp(1f - (fertDist + rainDist + tempDist + forestDist + heightDist) / 5f, 0f, 1f) * variant.Weight / 100f;
					distances.Add(variant, distance);
					total += distance;
				}
			}
		}
		distances = distances.Shuffle(rnd);
		double rng = rnd.NextDouble() * (double)total;
		foreach (KeyValuePair<TreeVariant, float> val in distances)
		{
			rng -= (double)val.Value;
			if (rng <= 0.001)
			{
				float suitabilityBonus = GameMath.Clamp(0.7f - val.Value, 0f, 0.7f) * 1f / 0.7f * val.Key.SuitabilitySizeBonus;
				float size = val.Key.MinSize + (float)rnd.NextDouble() * (val.Key.MaxSize - val.Key.MinSize) + suitabilityBonus;
				float descaledTemp = Climate.DescaleTemperature(temp);
				float rainVal = Math.Max(0f, ((float)rain / 255f - treeGenProps.vinesMinRain) / (1f - treeGenProps.vinesMinRain));
				float tempVal = Math.Max(0f, (descaledTemp / 255f - treeGenProps.descVineMinTempRel) / (1f - treeGenProps.descVineMinTempRel));
				float rainValMoss = (float)rain / 255f;
				float tempValMoss = descaledTemp / 255f;
				float vinesGrowthChance = 1.5f * rainVal * tempVal + 0.5f * rainVal * GameMath.Clamp((tempVal + 0.33f) / 1.33f, 0f, 1f);
				float mossGrowthChance = GameMath.Clamp((float)(2.25 * (double)rainValMoss - 0.5 + Math.Sqrt(tempValMoss) * 3.0 * Math.Max(-0.5, 0.5 - (double)tempValMoss)), 0f, 1f);
				ITreeGenerator treegen = treeGenerators.GetGenerator(val.Key.Generator);
				if (treegen == null)
				{
					api.World.Logger.Error("treengenproperties.json references tree generator {0}, but no such generator exists!", val.Key.Generator);
					return null;
				}
				return new TreeGenInstance
				{
					treeGen = treegen,
					size = size,
					vinesGrowthChance = vinesGrowthChance,
					mossGrowthChance = mossGrowthChance
				};
			}
		}
		return null;
	}
}
