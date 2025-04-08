using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class BlockPatchConfig
{
	[JsonProperty]
	public NatFloat ChanceMultiplier;

	[JsonProperty]
	public BlockPatch[] Patches;

	public BlockPatch[] PatchesNonTree;

	internal void ResolveBlockIds(ICoreServerAPI api, RockStrataConfig rockstrata, LCGRandom rnd)
	{
		List<BlockPatch> patchesNonTree = new List<BlockPatch>();
		for (int i = 0; i < Patches.Length; i++)
		{
			BlockPatch patch = Patches[i];
			if (patch.Placement != EnumBlockPatchPlacement.OnTrees && patch.Placement != EnumBlockPatchPlacement.UnderTrees)
			{
				patchesNonTree.Add(patch);
			}
			patch.Init(api, rockstrata, rnd, i);
		}
		PatchesNonTree = patchesNonTree.ToArray();
	}

	public bool IsPatchSuitableAt(BlockPatch patch, Block onBlock, int mapSizeY, int climate, int y, float forestRel, float shrubRel)
	{
		if ((patch.Placement == EnumBlockPatchPlacement.NearWater || patch.Placement == EnumBlockPatchPlacement.UnderWater) && onBlock.LiquidCode != "water")
		{
			return false;
		}
		if ((patch.Placement == EnumBlockPatchPlacement.NearSeaWater || patch.Placement == EnumBlockPatchPlacement.UnderSeaWater) && onBlock.LiquidCode != "saltwater")
		{
			return false;
		}
		if (forestRel < patch.MinForest || forestRel > patch.MaxForest || shrubRel < patch.MinShrub || forestRel > patch.MaxShrub)
		{
			return false;
		}
		int rain = Climate.GetRainFall((climate >> 8) & 0xFF, y);
		float rainRel = (float)rain / 255f;
		if (rainRel < patch.MinRain || rainRel > patch.MaxRain)
		{
			return false;
		}
		int temp = Climate.GetScaledAdjustedTemperature((climate >> 16) & 0xFF, y - TerraGenConfig.seaLevel);
		if (temp < patch.MinTemp || temp > patch.MaxTemp)
		{
			return false;
		}
		float sealevelDistRel = ((float)y - (float)TerraGenConfig.seaLevel) / ((float)mapSizeY - (float)TerraGenConfig.seaLevel);
		if (sealevelDistRel < patch.MinY || sealevelDistRel > patch.MaxY)
		{
			return false;
		}
		float fertilityRel = (float)Climate.GetFertility(rain, temp, sealevelDistRel) / 255f;
		if (fertilityRel >= patch.MinFertility)
		{
			return fertilityRel <= patch.MaxFertility;
		}
		return false;
	}

	public bool IsPatchSuitableUnderTree(BlockPatch patch, int mapSizeY, ClimateCondition climate, int y)
	{
		float rainRel = climate.Rainfall;
		if (rainRel < patch.MinRain || rainRel > patch.MaxRain)
		{
			return false;
		}
		float temp = climate.Temperature;
		if (temp < (float)patch.MinTemp || temp > (float)patch.MaxTemp)
		{
			return false;
		}
		float sealevelDistRel = ((float)y - (float)TerraGenConfig.seaLevel) / ((float)mapSizeY - (float)TerraGenConfig.seaLevel);
		if (sealevelDistRel < patch.MinY || sealevelDistRel > patch.MaxY)
		{
			return false;
		}
		float fertilityRel = climate.Fertility;
		if (fertilityRel >= patch.MinFertility)
		{
			return fertilityRel <= patch.MaxFertility;
		}
		return false;
	}
}
