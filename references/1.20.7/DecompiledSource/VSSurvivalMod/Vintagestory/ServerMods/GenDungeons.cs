using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class GenDungeons : ModStdWorldGen
{
	private ModSystemTiledDungeonGenerator dungeonGen;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private LCGRandom rand;

	private ICoreServerAPI api;

	private Dictionary<long, List<DungeonPlaceTask>> dungeonPlaceTasksByRegion = new Dictionary<long, List<DungeonPlaceTask>>();

	private int regionSize;

	private bool genDungeons;

	public override double ExecuteOrder()
	{
		return 0.12;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		dungeonGen = api.ModLoader.GetModSystem<ModSystemTiledDungeonGenerator>();
		dungeonGen.init();
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		genDungeons = api.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true);
		if (genDungeons)
		{
			worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
			rand = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
			regionSize = api.World.BlockAccessor.RegionSize;
		}
	}

	private void Event_MapRegionLoaded(Vec2i mapCoord, IMapRegion region)
	{
		List<DungeonPlaceTask> tasks = region.GetModdata<List<DungeonPlaceTask>>("dungeonPlaceTasks");
		if (tasks != null)
		{
			dungeonPlaceTasksByRegion[MapRegionIndex2D(mapCoord.X, mapCoord.Y)] = tasks;
		}
	}

	private void Event_MapRegionUnloaded(Vec2i mapCoord, IMapRegion region)
	{
		if (dungeonPlaceTasksByRegion.TryGetValue(MapRegionIndex2D(mapCoord.X, mapCoord.Y), out var list) && list != null)
		{
			region.SetModdata("dungeonPlaceTasks", list);
		}
	}

	private void onMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int size = api.WorldManager.RegionSize;
		LCGRandom rand2 = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
		rand2.InitPositionSeed(regionX * size, regionZ * size);
		long index = MapRegionIndex2D(regionX, regionZ);
		dungeonPlaceTasksByRegion[index] = new List<DungeonPlaceTask>();
		for (int i = 0; i < 3; i++)
		{
			int posx = regionX * size + rand2.NextInt(size);
			int posz = regionZ * size + rand2.NextInt(size);
			int posy = rand2.NextInt(api.World.SeaLevel - 10);
			api.Logger.Event($"Dungeon @: /tp ={posx} {posy} ={posz}");
			TiledDungeon dungeon = dungeonGen.Tcfg.Dungeons[0].Copy();
			DungeonPlaceTask placeTask = dungeonGen.TryPregenerateTiledDungeon(rand2, dungeon, new BlockPos(posx, posy, posz), 5, 50);
			if (placeTask != null)
			{
				dungeonPlaceTasksByRegion[index].Add(placeTask);
			}
		}
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)(api.WorldManager.MapSizeX / api.WorldManager.RegionSize) + regionX;
	}

	private void onChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		int regionx = request.ChunkX * 32 / regionSize;
		int regionz = request.ChunkZ * 32 / regionSize;
		int posX = request.ChunkX * 32;
		int posZ = request.ChunkZ * 32;
		Cuboidi cuboid = new Cuboidi(posX, 0, posZ, posX + 32, api.World.BlockAccessor.MapSizeY, posZ + 32);
		Cuboidi baseRoom = new Cuboidi();
		IMapRegion region = request.Chunks[0].MapChunk.MapRegion;
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				if (!dungeonPlaceTasksByRegion.TryGetValue(MapRegionIndex2D(regionx + dx, regionz + dz), out var dungePlaceTasks))
				{
					continue;
				}
				foreach (DungeonPlaceTask placetask in dungePlaceTasks)
				{
					if (!placetask.DungeonBoundaries.IntersectsOrTouches(cuboid))
					{
						continue;
					}
					TilePlaceTask tileTask = placetask.TilePlaceTasks[0];
					baseRoom.Set(tileTask.Pos, tileTask.Pos.AddCopy(tileTask.SizeX, tileTask.SizeY, tileTask.SizeZ));
					if (cuboid.IntersectsOrTouches(baseRoom))
					{
						if (!dungeonGen.Tcfg.DungeonsByCode.TryGetValue(placetask.Code, out var dungeon))
						{
							return;
						}
						int num = (api.World.BlockAccessor.GetTerrainMapheightAt(tileTask.Pos) - tileTask.Pos.Y) / dungeon.Stairs[0].SizeY;
						BlockPos stairPos = tileTask.Pos.AddCopy(tileTask.SizeX / 2 - dungeon.Stairs[0].SizeX / 2, tileTask.SizeY, tileTask.SizeZ / 2 - dungeon.Stairs[0].SizeZ / 2);
						for (int i = 0; i < num; i++)
						{
							dungeon.Stairs[0].Place(worldgenBlockAccessor, api.World, stairPos);
							stairPos.Y += dungeon.Stairs[0].SizeY;
						}
						region.AddGeneratedStructure(new GeneratedStructure
						{
							Code = "dungeon/" + dungeon.Code + "/" + dungeon.Stairs[0].FromFileName,
							Group = placetask.Code,
							Location = new Cuboidi(tileTask.Pos.AddCopy(0, tileTask.SizeY, 0), stairPos.AddCopy(dungeon.Stairs[0].SizeX, 0, dungeon.Stairs[0].SizeZ)),
							SuppressTreesAndShrubs = true,
							SuppressRivulets = true
						});
					}
					generateDungeonPartial(region, placetask, request.Chunks, request.ChunkX, request.ChunkZ);
				}
			}
		}
	}

	private void generateDungeonPartial(IMapRegion region, DungeonPlaceTask dungeonPlaceTask, IServerChunk[] chunks, int chunkX, int chunkZ)
	{
		if (!dungeonGen.Tcfg.DungeonsByCode.TryGetValue(dungeonPlaceTask.Code, out var dungeon))
		{
			return;
		}
		LCGRandom rand2 = new LCGRandom(api.WorldManager.Seed ^ 0x217F464FEL);
		int size = api.WorldManager.RegionSize;
		rand2.InitPositionSeed(chunkX / size * size, chunkZ / size * size);
		foreach (TilePlaceTask placeTask in dungeonPlaceTask.TilePlaceTasks)
		{
			if (dungeon.TilesByCode.TryGetValue(placeTask.TileCode, out var tile))
			{
				int rndval = rand2.NextInt(tile.ResolvedSchematic.Length);
				BlockSchematicPartial schematic = tile.ResolvedSchematic[rndval][placeTask.Rotation];
				schematic.PlacePartial(chunks, worldgenBlockAccessor, api.World, chunkX, chunkZ, placeTask.Pos, EnumReplaceMode.ReplaceAll, null, replaceMeta: true, resolveImports: true);
				string code = "dungeon/" + tile.Code + ((schematic == null) ? "" : ("/" + schematic.FromFileName));
				region.AddGeneratedStructure(new GeneratedStructure
				{
					Code = code,
					Group = dungeonPlaceTask.Code,
					Location = new Cuboidi(placeTask.Pos, placeTask.Pos.AddCopy(schematic.SizeX, schematic.SizeY, schematic.SizeZ)),
					SuppressTreesAndShrubs = true,
					SuppressRivulets = true
				});
			}
		}
	}
}
