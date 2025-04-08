using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenStoryStructure : WorldGenStructureBase
{
	[JsonProperty]
	public string Group;

	[JsonProperty]
	public string RequireLandform;

	[JsonProperty]
	public int LandformRadius;

	[JsonProperty]
	public int GenerationRadius;

	[JsonProperty]
	public string DependsOnStructure;

	[JsonProperty]
	public int MinSpawnDistX;

	[JsonProperty]
	public int MaxSpawnDistX;

	[JsonProperty]
	public int MinSpawnDistZ;

	[JsonProperty]
	public int MaxSpawnDistZ;

	[JsonProperty]
	public int ExtraLandClaimX;

	[JsonProperty]
	public int ExtraLandClaimZ;

	[JsonProperty]
	public Dictionary<string, int> SkipGenerationCategories;

	public Dictionary<int, int> SkipGenerationFlags;

	[JsonProperty]
	public int? ForceRain;

	[JsonProperty]
	public int? ForceTemperature;

	[JsonProperty]
	public bool GenerateGrass;

	[JsonProperty]
	public Cuboidi[] CustomLandClaims;

	[JsonProperty]
	public bool ExcludeSchematicSizeProtect;

	internal BlockSchematicPartial schematicData;

	[JsonProperty]
	public AssetLocation[] ReplaceWithBlocklayers;

	internal int[] replacewithblocklayersBlockids = new int[0];

	internal Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps;

	public void Init(ICoreServerAPI api, WorldGenStoryStructuresConfig scfg, RockStrataConfig rockstrata, BlockLayerConfig blockLayerConfig)
	{
		schematicData = LoadSchematics<BlockSchematicPartial>(api, Schematics, null)[0];
		schematicData.blockLayerConfig = blockLayerConfig;
		scfg.SchematicYOffsets.TryGetValue("story/" + schematicData.FromFileName.Replace(".json", ""), out var offset);
		schematicData.OffsetY = offset;
		if (SkipGenerationCategories != null)
		{
			SkipGenerationFlags = new Dictionary<int, int>();
			foreach (KeyValuePair<string, int> category in SkipGenerationCategories)
			{
				SkipGenerationFlags.Add(BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(category.Key.ToLowerInvariant()))), category.Value);
			}
		}
		if (RockTypeRemapGroup != null)
		{
			resolvedRockTypeRemaps = scfg.resolvedRocktypeRemapGroups[RockTypeRemapGroup];
		}
		if (RockTypeRemaps != null)
		{
			if (resolvedRockTypeRemaps != null)
			{
				Dictionary<int, Dictionary<int, int>> ownRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
				foreach (KeyValuePair<int, Dictionary<int, int>> val in resolvedRockTypeRemaps)
				{
					ownRemaps[val.Key] = val.Value;
				}
				resolvedRockTypeRemaps = ownRemaps;
			}
			else
			{
				resolvedRockTypeRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
			}
		}
		if (ReplaceWithBlocklayers == null)
		{
			return;
		}
		replacewithblocklayersBlockids = new int[ReplaceWithBlocklayers.Length];
		for (int i = 0; i < replacewithblocklayersBlockids.Length; i++)
		{
			Block block = api.World.GetBlock(ReplaceWithBlocklayers[i]);
			if (block == null)
			{
				throw new Exception($"Schematic with code {Code} has replace block layer {ReplaceWithBlocklayers[i]} defined, but no such block found!");
			}
			replacewithblocklayersBlockids[i] = block.Id;
		}
	}
}
