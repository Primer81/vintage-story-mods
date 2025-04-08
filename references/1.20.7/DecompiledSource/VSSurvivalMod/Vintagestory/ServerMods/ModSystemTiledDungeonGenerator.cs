using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Collectible.Block;

namespace Vintagestory.ServerMods;

public class ModSystemTiledDungeonGenerator : ModSystem
{
	protected ICoreServerAPI api;

	public TiledDungeonConfig Tcfg;

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("debug").BeginSub("tiledd").WithDesc("Tiled dungeon generator debugger/tester")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(parsers.Word("tiled dungeon code"), parsers.Int("amount of tiles"))
			.HandleWith(OnCmdTiledCungeonCode)
			.EndSub()
			.BeginSub("tileddd")
			.WithDesc("Tiled dungeon generator debugger/tester")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(parsers.Word("tiled dungeon code"))
			.HandleWith(OnCmdTiledCungeonTest)
			.EndSub();
	}

	private TextCommandResult OnCmdTiledCungeonTest(TextCommandCallingArgs args)
	{
		api.Assets.Reload(AssetCategory.worldgen);
		init();
		string code = (string)args[0];
		TiledDungeon dungeon = Tcfg.Dungeons.FirstOrDefault((TiledDungeon td) => td.Code == code).Copy();
		if (dungeon == null)
		{
			return TextCommandResult.Error("No such dungeon defined");
		}
		BlockPos pos = args.Caller.Pos.AsBlockPos;
		pos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(pos) + 30;
		IBlockAccessor ba = api.World.BlockAccessor;
		int pathwayBlockId = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-connector")).BlockId;
		int orignalX = pos.X;
		foreach (KeyValuePair<string, DungeonTile> item in dungeon.TilesByCode)
		{
			item.Deconstruct(out var _, out var value);
			DungeonTile dungeonTile = value;
			for (int i = 0; i < 4; i++)
			{
				List<BlockPosFacing> blockPosFacings = dungeonTile.ResolvedSchematic[0][i].PathwayBlocksUnpacked;
				dungeonTile.ResolvedSchematic[0][i].Place(ba, api.World, pos);
				dungeonTile.ResolvedSchematic[0][i].PlaceEntitiesAndBlockEntities(ba, api.World, pos, new Dictionary<int, AssetLocation>(), new Dictionary<int, AssetLocation>());
				foreach (BlockPosFacing item2 in blockPosFacings)
				{
					ba.SetBlock(pathwayBlockId, pos + item2.Position);
				}
				pos.X += 30;
			}
			pos.Z += 30;
			pos.X = orignalX;
		}
		return TextCommandResult.Success("dungeon generated");
	}

	internal void init()
	{
		IAsset asset = api.Assets.Get("worldgen/tileddungeons.json");
		Tcfg = asset.ToObject<TiledDungeonConfig>();
		Tcfg.Init(api);
	}

	private TextCommandResult OnCmdTiledCungeonCode(TextCommandCallingArgs args)
	{
		api.Assets.Reload(AssetCategory.worldgen);
		init();
		string code = (string)args[0];
		int tiles = (int)args[1];
		TiledDungeon dungeon = Tcfg.Dungeons.FirstOrDefault((TiledDungeon td) => td.Code == code).Copy();
		if (dungeon == null)
		{
			return TextCommandResult.Error("No such dungeon defined");
		}
		BlockPos pos = args.Caller.Pos.AsBlockPos;
		pos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(pos) + 30;
		IBlockAccessor ba = api.World.BlockAccessor;
		int size = api.WorldManager.RegionSize;
		LCGRandom rnd = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
		rnd.InitPositionSeed(pos.X / size * size, pos.Z / size * size);
		for (int i = 0; i < 50; i++)
		{
			if (TryPlaceTiledDungeon(ba, rnd, dungeon, pos, tiles, tiles))
			{
				return TextCommandResult.Success("dungeon generated");
			}
		}
		return TextCommandResult.Error("Unable to generate dungeon of this size after 50 attempts");
	}

	public DungeonPlaceTask TryPregenerateTiledDungeon(IRandom rnd, TiledDungeon dungeon, BlockPos startPos, int minTiles, int maxTiles)
	{
		int rot = rnd.NextInt(4);
		Queue<BlockPosFacing> openSet = new Queue<BlockPosFacing>();
		List<TilePlaceTask> placeTasks = new List<TilePlaceTask>();
		List<GeneratedStructure> gennedStructures = new List<GeneratedStructure>();
		BlockSchematicPartial[] btile = ((dungeon.Start != null) ? dungeon.Start : dungeon.TilesByCode["4way"].ResolvedSchematic[0]);
		string startCode = ((dungeon.Start != null) ? dungeon.start : "4way");
		Cuboidi loc = place(btile, startCode, rot, startPos, openSet, placeTasks);
		gennedStructures.Add(new GeneratedStructure
		{
			Code = "dungeon-" + startCode,
			Location = loc,
			SuppressRivulets = true
		});
		int tries = minTiles * 10;
		while (tries-- > 0 && openSet.Count > 0)
		{
			BlockPosFacing openside = openSet.Dequeue();
			dungeon.Tiles.Shuffle(rnd);
			float rndval = (float)rnd.NextDouble() * dungeon.totalChance;
			int cnt = dungeon.Tiles.Count;
			int skipped = 0;
			bool maxTilesReached = placeTasks.Count >= maxTiles;
			if (maxTilesReached)
			{
				rndval = 0f;
			}
			for (int j = 0; j < cnt + skipped; j++)
			{
				DungeonTile tile = dungeon.Tiles[j % cnt];
				if (!tile.IgnoreMaxTiles && maxTilesReached)
				{
					continue;
				}
				rndval -= tile.Chance;
				if (rndval > 0f)
				{
					skipped++;
				}
				else
				{
					if (!tile.ResolvedSchematic[0].Any((BlockSchematicPartial s) => s.PathwayBlocksUnpacked.Any((BlockPosFacing p) => openside.Facing.Opposite == p.Facing && WildcardUtil.Match(openside.Constraints, tile.Code))))
					{
						continue;
					}
					int startRot = rnd.NextInt(4);
					rot = 0;
					BlockFacing attachingFace = openside.Facing.Opposite;
					bool ok = false;
					BlockPos offsetPos = null;
					BlockSchematicPartial schematic = null;
					for (int i = 0; i < 4; i++)
					{
						rot = (startRot + i) % 4;
						schematic = tile.ResolvedSchematic[0][rot];
						if (schematic.PathwayBlocksUnpacked.Any((BlockPosFacing p) => p.Facing == attachingFace && WildcardUtil.Match(openside.Constraints, tile.Code)))
						{
							offsetPos = schematic.PathwayBlocksUnpacked.First((BlockPosFacing p) => p.Facing == attachingFace && WildcardUtil.Match(openside.Constraints, tile.Code)).Position;
							ok = true;
							break;
						}
					}
					if (ok)
					{
						BlockPos pos = openside.Position.Copy();
						pos = pos.AddCopy(openside.Facing) - offsetPos;
						Cuboidi newloc = new Cuboidi(pos.X, pos.Y, pos.Z, pos.X + schematic.SizeX, pos.Y + schematic.SizeY, pos.Z + schematic.SizeZ);
						if (!intersects(gennedStructures, newloc))
						{
							loc = place(tile, rot, pos, openSet, placeTasks, openside.Facing.Opposite);
							gennedStructures.Add(new GeneratedStructure
							{
								Code = tile.Code,
								Location = loc,
								SuppressRivulets = true
							});
							break;
						}
					}
				}
			}
		}
		if (placeTasks.Count >= minTiles)
		{
			return new DungeonPlaceTask
			{
				Code = dungeon.Code,
				TilePlaceTasks = placeTasks
			}.GenBoundaries();
		}
		return null;
	}

	public bool TryPlaceTiledDungeon(IBlockAccessor ba, IRandom rnd, TiledDungeon dungeon, BlockPos startPos, int minTiles, int maxTiles)
	{
		DungeonPlaceTask dungenPlaceTask = TryPregenerateTiledDungeon(rnd, dungeon, startPos, minTiles, maxTiles);
		if (dungenPlaceTask != null)
		{
			foreach (TilePlaceTask placeTask in dungenPlaceTask.TilePlaceTasks)
			{
				if (dungeon.TilesByCode.TryGetValue(placeTask.TileCode, out var tile))
				{
					int rndval = rnd.NextInt(tile.ResolvedSchematic.Length);
					tile.ResolvedSchematic[rndval][placeTask.Rotation].Place(ba, api.World, placeTask.Pos);
				}
			}
			return true;
		}
		return false;
	}

	protected bool intersects(List<GeneratedStructure> gennedStructures, Cuboidi newloc)
	{
		for (int i = 0; i < gennedStructures.Count; i++)
		{
			if (gennedStructures[i].Location.Intersects(newloc))
			{
				return true;
			}
		}
		return false;
	}

	protected Cuboidi place(DungeonTile tile, int rot, BlockPos startPos, Queue<BlockPosFacing> openSet, List<TilePlaceTask> placeTasks, BlockFacing attachingFace = null)
	{
		BlockSchematicPartial[] schematics = tile.ResolvedSchematic[0];
		return place(schematics, tile.Code, rot, startPos, openSet, placeTasks, attachingFace);
	}

	protected Cuboidi place(BlockSchematicPartial[] schematics, string code, int rot, BlockPos startPos, Queue<BlockPosFacing> openSet, List<TilePlaceTask> placeTasks, BlockFacing attachingFace = null)
	{
		BlockSchematicPartial schematic = schematics[rot];
		placeTasks.Add(new TilePlaceTask
		{
			TileCode = code,
			Rotation = rot,
			Pos = startPos.Copy(),
			SizeX = schematic.SizeX,
			SizeY = schematic.SizeY,
			SizeZ = schematic.SizeZ
		});
		foreach (BlockPosFacing path in schematic.PathwayBlocksUnpacked)
		{
			if (path.Facing != attachingFace)
			{
				openSet.Enqueue(new BlockPosFacing(path.Position + startPos, path.Facing, path.Constraints));
			}
		}
		return new Cuboidi(startPos.X, startPos.Y, startPos.Z, startPos.X + schematic.SizeX, startPos.Y + schematic.SizeY, startPos.Z + schematic.SizeZ);
	}

	private string[][] rotate(int rot, string[][] constraints)
	{
		return new string[6][]
		{
			constraints[(-rot + 4) % 4],
			constraints[(1 - rot + 4) % 4],
			constraints[(2 - rot + 4) % 4],
			constraints[(3 - rot + 4) % 4],
			constraints[4],
			constraints[5]
		};
	}
}
