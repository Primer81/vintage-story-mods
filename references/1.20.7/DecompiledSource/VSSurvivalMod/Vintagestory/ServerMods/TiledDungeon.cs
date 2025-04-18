using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class TiledDungeon
{
	public string Code;

	public List<DungeonTile> Tiles = new List<DungeonTile>();

	public Dictionary<string, DungeonTile> TilesByCode = new Dictionary<string, DungeonTile>();

	public float totalChance;

	public string stairs;

	[JsonIgnore]
	public BlockSchematicPartial[] Stairs;

	public string start;

	[JsonIgnore]
	public BlockSchematicPartial[] Start;

	internal void Init(ICoreServerAPI api)
	{
		totalChance = 0f;
		BlockLayerConfig blockLayerConfig = BlockLayerConfig.GetInstance(api);
		blockLayerConfig.ResolveBlockIds(api);
		if (stairs != null)
		{
			IAsset asset = api.Assets.Get("worldgen/dungeontiles/" + stairs + ".json");
			Stairs = WorldGenStructureBase.LoadSchematic<BlockSchematicPartial>(api, asset, blockLayerConfig, null, null, 0);
		}
		if (start != null)
		{
			IAsset assetStart = api.Assets.Get("worldgen/dungeontiles/" + start + ".json");
			Start = WorldGenStructureBase.LoadSchematic<BlockSchematicPartial>(api, assetStart, blockLayerConfig, null, null, 0, isDungeon: true);
			TilesByCode[start] = new DungeonTile
			{
				ResolvedSchematic = new BlockSchematicPartial[1][] { Start }
			};
		}
		for (int i = 0; i < Tiles.Count; i++)
		{
			Tiles[i].Init(api, blockLayerConfig);
			TilesByCode[Tiles[i].Code] = Tiles[i];
			totalChance += Tiles[i].Chance;
		}
	}

	public TiledDungeon Copy()
	{
		return new TiledDungeon
		{
			totalChance = totalChance,
			Tiles = new List<DungeonTile>(Tiles),
			Code = Code,
			TilesByCode = new Dictionary<string, DungeonTile>(TilesByCode),
			Stairs = Stairs,
			stairs = stairs,
			Start = Start,
			start = start
		};
	}
}
