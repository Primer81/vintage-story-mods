using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenStoryStructuresConfig : WorldGenStructuresConfigBase
{
	[JsonProperty]
	public WorldGenStoryStructure[] Structures;

	public void Init(ICoreServerAPI api, RockStrataConfig rockstrata, BlockLayerConfig blockLayerConfig)
	{
		ResolveRemaps(api, rockstrata);
		WorldGenStoryStructure[] structures = Structures;
		for (int i = 0; i < structures.Length; i++)
		{
			structures[i].Init(api, this, rockstrata, blockLayerConfig);
		}
	}
}
