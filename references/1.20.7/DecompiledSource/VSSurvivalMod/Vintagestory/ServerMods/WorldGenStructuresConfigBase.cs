using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenStructuresConfigBase
{
	[JsonProperty]
	public Dictionary<string, Dictionary<AssetLocation, AssetLocation>> RocktypeRemapGroups;

	[JsonProperty]
	public Dictionary<string, int> SchematicYOffsets;

	public Dictionary<string, Dictionary<int, Dictionary<int, int>>> resolvedRocktypeRemapGroups;

	public void ResolveRemaps(ICoreServerAPI api, RockStrataConfig rockstrata)
	{
		if (RocktypeRemapGroups == null)
		{
			return;
		}
		resolvedRocktypeRemapGroups = new Dictionary<string, Dictionary<int, Dictionary<int, int>>>();
		foreach (KeyValuePair<string, Dictionary<AssetLocation, AssetLocation>> val in RocktypeRemapGroups)
		{
			resolvedRocktypeRemapGroups[val.Key] = ResolveRockTypeRemaps(val.Value, rockstrata, api);
		}
	}

	public static Dictionary<int, Dictionary<int, int>> ResolveRockTypeRemaps(Dictionary<AssetLocation, AssetLocation> rockTypeRemaps, RockStrataConfig rockstrata, ICoreAPI api)
	{
		Dictionary<int, Dictionary<int, int>> resolvedReplaceWithRocktype = new Dictionary<int, Dictionary<int, int>>();
		foreach (KeyValuePair<AssetLocation, AssetLocation> val in rockTypeRemaps)
		{
			RockStratum[] variants;
			Block[] array;
			if (val.Key.Path.Contains("*"))
			{
				array = api.World.SearchBlocks(val.Key);
				foreach (Block block in array)
				{
					Dictionary<int, int> blockIdByRockId = new Dictionary<int, int>();
					variants = rockstrata.Variants;
					foreach (RockStratum strat in variants)
					{
						Block rockBlock = api.World.GetBlock(strat.BlockCode);
						AssetLocation resolvedLoc = block.CodeWithVariant("rock", rockBlock.LastCodePart());
						Block resolvedBlock = api.World.GetBlock(resolvedLoc);
						if (resolvedBlock != null)
						{
							blockIdByRockId[rockBlock.Id] = resolvedBlock.Id;
						}
					}
					resolvedReplaceWithRocktype[block.Id] = blockIdByRockId;
				}
				continue;
			}
			Dictionary<int, int> blockIdByRockId2 = new Dictionary<int, int>();
			variants = rockstrata.Variants;
			foreach (RockStratum strat2 in variants)
			{
				Block rockBlock2 = api.World.GetBlock(strat2.BlockCode);
				AssetLocation resolvedLoc2 = val.Value.Clone();
				resolvedLoc2.Path = resolvedLoc2.Path.Replace("{rock}", rockBlock2.LastCodePart());
				Block resolvedBlock2 = api.World.GetBlock(resolvedLoc2);
				if (resolvedBlock2 != null)
				{
					blockIdByRockId2[rockBlock2.Id] = resolvedBlock2.Id;
					Block quartzBlock = api.World.GetBlock(new AssetLocation("ore-quartz-" + rockBlock2.LastCodePart()));
					if (quartzBlock != null)
					{
						blockIdByRockId2[quartzBlock.Id] = resolvedBlock2.Id;
					}
				}
			}
			array = api.World.SearchBlocks(val.Key);
			foreach (Block sourceBlock in array)
			{
				resolvedReplaceWithRocktype[sourceBlock.Id] = blockIdByRockId2;
			}
		}
		return resolvedReplaceWithRocktype;
	}
}
