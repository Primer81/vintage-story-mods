using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenStructuresConfig : WorldGenStructuresConfigBase
{
	[JsonProperty]
	public float ChanceMultiplier;

	[JsonProperty]
	public WorldGenStructure[] Structures;

	private BlockLayerConfig blockLayerConfig;

	public Dictionary<string, BlockSchematicStructure[]> LoadedSchematicsCache;

	internal void Init(ICoreServerAPI api)
	{
		blockLayerConfig = BlockLayerConfig.GetInstance(api);
		ResolveRemaps(api, blockLayerConfig.RockStrata);
		LoadedSchematicsCache = new Dictionary<string, BlockSchematicStructure[]>();
		for (int i = 0; i < Structures.Length; i++)
		{
			LCGRandom rand = new LCGRandom(api.World.Seed + i + 512);
			try
			{
				Structures[i].Init(api, blockLayerConfig, blockLayerConfig.RockStrata, this, rand);
			}
			catch (Exception e)
			{
				api.Logger.Error("The following exception occurred while initialising structure for worldgen: " + Structures[i].Code);
				api.Logger.Error(e);
			}
		}
	}
}
