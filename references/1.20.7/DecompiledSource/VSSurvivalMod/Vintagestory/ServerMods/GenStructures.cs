using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class GenStructures : ModStdWorldGen
{
	public static bool ReplaceMetaBlocks = true;

	private ICoreServerAPI api;

	private int worldheight;

	private int regionChunkSize;

	private ushort[] heightmap;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private WorldGenStructuresConfig scfg;

	public WorldGenVillageConfig vcfg;

	private LCGRandom strucRand;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private WorldGenStructure[] shuffledStructures;

	private Dictionary<string, WorldGenStructure[]> StoryStructures;

	private BlockPos spawnPos;

	public event PeventSchematicAtDelegate OnPreventSchematicPlaceAt;

	public override double ExecuteOrder()
	{
		return 0.3;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.ModLoader.GetModSystem<GenStructuresPosPass>().handler = OnChunkColumnGenPostPass;
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public bool WouldSchematicOverlapAt(IBlockAccessor blockAccessor, BlockPos pos, Cuboidi schematicLocation, string locationCode)
	{
		if (this.OnPreventSchematicPlaceAt != null)
		{
			Delegate[] invocationList = this.OnPreventSchematicPlaceAt.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				if (((PeventSchematicAtDelegate)invocationList[i])(blockAccessor, pos, schematicLocation, locationCode))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		Block fillerBlock = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-filler"));
		Block pathwayBlock = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-pathway"));
		Block undergroundBlock = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-underground"));
		Block block = api.World.BlockAccessor.GetBlock(new AssetLocation("meta-aboveground"));
		BlockSchematic.FillerBlockId = fillerBlock.Id;
		BlockSchematic.PathwayBlockId = pathwayBlock.Id;
		BlockSchematic.UndergroundBlockId = undergroundBlock.Id;
		BlockSchematic.AbovegroundBlockId = block.Id;
		worldheight = api.WorldManager.MapSizeY;
		regionChunkSize = api.WorldManager.RegionSize / 32;
		strucRand = new LCGRandom(api.WorldManager.Seed + 1090);
		IAsset asset = api.Assets.Get("worldgen/structures.json");
		scfg = asset.ToObject<WorldGenStructuresConfig>();
		shuffledStructures = new WorldGenStructure[scfg.Structures.Length];
		scfg.Init(api);
		asset = api.Assets.Get("worldgen/villages.json");
		vcfg = asset.ToObject<WorldGenVillageConfig>();
		vcfg.Init(api, scfg);
		if (!api.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true))
		{
			return;
		}
		asset = api.Assets.Get("worldgen/storystructures.json");
		WorldGenStoryStructuresConfig worldGenStoryStructuresConfig = asset.ToObject<WorldGenStoryStructuresConfig>();
		StoryStructures = new Dictionary<string, WorldGenStructure[]>();
		WorldGenStoryStructure[] structures = worldGenStoryStructuresConfig.Structures;
		foreach (WorldGenStoryStructure storyStructure in structures)
		{
			string path = "worldgen/story/" + storyStructure.Code + "/structures.json";
			if (api.Assets.Exists(new AssetLocation(path)))
			{
				asset = api.Assets.Get(path);
				WorldGenStructuresConfig storyStructuresConfig = asset.ToObject<WorldGenStructuresConfig>();
				storyStructuresConfig.Init(api);
				StoryStructures[storyStructure.Code] = storyStructuresConfig.Structures;
			}
		}
		PlayerSpawnPos df = api.WorldManager.SaveGame.DefaultSpawn;
		if (df != null)
		{
			spawnPos = new BlockPos(df.x, df.y.GetValueOrDefault(), df.z);
		}
		else
		{
			spawnPos = api.World.BlockAccessor.MapSize.AsBlockPos / 2;
		}
	}

	private void OnChunkColumnGenPostPass(IChunkColumnGenerateRequest request)
	{
		if (TerraGenConfig.GenerateStructures)
		{
			string locationCode = GetIntersectingStructure(request.ChunkX * 32 + 16, request.ChunkZ * 32 + 16, ModStdWorldGen.SkipStructuresgHashCode);
			IServerChunk[] chunks = request.Chunks;
			int chunkX = request.ChunkX;
			int chunkZ = request.ChunkZ;
			worldgenBlockAccessor.BeginColumn();
			IMapRegion region = chunks[0].MapChunk.MapRegion;
			DoGenStructures(region, chunkX, chunkZ, postPass: true, locationCode, request.ChunkGenParams);
			TryGenVillages(region, chunkX, chunkZ, postPass: true, request.ChunkGenParams);
		}
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		if (TerraGenConfig.GenerateStructures)
		{
			string locationCode = GetIntersectingStructure(request.ChunkX * 32 + 16, request.ChunkZ * 32 + 16, ModStdWorldGen.SkipStructuresgHashCode);
			IServerChunk[] chunks = request.Chunks;
			int chunkX = request.ChunkX;
			int chunkZ = request.ChunkZ;
			worldgenBlockAccessor.BeginColumn();
			IMapRegion region = chunks[0].MapChunk.MapRegion;
			IntDataMap2D climateMap = region.ClimateMap;
			int rlX = chunkX % regionChunkSize;
			int rlZ = chunkZ % regionChunkSize;
			float facC = (float)climateMap.InnerSize / (float)regionChunkSize;
			climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC));
			climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC));
			climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC + facC));
			climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC + facC));
			heightmap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
			DoGenStructures(region, chunkX, chunkZ, postPass: false, locationCode, request.ChunkGenParams);
			if (locationCode == null)
			{
				TryGenVillages(region, chunkX, chunkZ, postPass: false, request.ChunkGenParams);
			}
		}
	}

	private void DoGenStructures(IMapRegion region, int chunkX, int chunkZ, bool postPass, string locationCode, ITreeAttribute chunkGenParams = null)
	{
		if (locationCode != null)
		{
			if (!StoryStructures.TryGetValue(locationCode, out var storyStructures))
			{
				return;
			}
			shuffledStructures = new WorldGenStructure[storyStructures.Length];
			for (int j = 0; j < storyStructures.Length; j++)
			{
				shuffledStructures[j] = storyStructures[j];
			}
		}
		else
		{
			shuffledStructures = new WorldGenStructure[scfg.Structures.Length];
			for (int k = 0; k < shuffledStructures.Length; k++)
			{
				shuffledStructures[k] = scfg.Structures[k];
			}
		}
		BlockPos startPos = new BlockPos();
		ITreeAttribute chanceModTree = null;
		ITreeAttribute maxQuantityModTree = null;
		StoryStructureLocation location = null;
		if (chunkGenParams?["structureChanceModifier"] != null)
		{
			chanceModTree = chunkGenParams["structureChanceModifier"] as TreeAttribute;
		}
		if (chunkGenParams?["structureMaxCount"] != null)
		{
			maxQuantityModTree = chunkGenParams["structureMaxCount"] as TreeAttribute;
		}
		strucRand.InitPositionSeed(chunkX, chunkZ);
		shuffledStructures.Shuffle(strucRand);
		for (int i = 0; i < shuffledStructures.Length; i++)
		{
			WorldGenStructure struc = shuffledStructures[i];
			if (struc.PostPass != postPass)
			{
				continue;
			}
			float chance = struc.Chance * scfg.ChanceMultiplier;
			int toGenerate = 9999;
			if (chanceModTree != null)
			{
				chance *= chanceModTree.GetFloat(struc.Code);
			}
			if (maxQuantityModTree != null)
			{
				toGenerate = maxQuantityModTree.GetInt(struc.Code, 9999);
			}
			while (chance-- > strucRand.NextFloat() && toGenerate > 0)
			{
				int dx = strucRand.NextInt(32);
				int dz = strucRand.NextInt(32);
				int ySurface = heightmap[dz * 32 + dx];
				if (ySurface <= 0 || ySurface >= worldheight - 15)
				{
					continue;
				}
				if (struc.Placement == EnumStructurePlacement.Underground)
				{
					if (struc.Depth != null)
					{
						startPos.Set(chunkX * 32 + dx, ySurface - (int)struc.Depth.nextFloat(1f, strucRand), chunkZ * 32 + dz);
					}
					else
					{
						startPos.Set(chunkX * 32 + dx, 8 + strucRand.NextInt(Math.Max(1, ySurface - 8 - 5)), chunkZ * 32 + dz);
					}
				}
				else
				{
					startPos.Set(chunkX * 32 + dx, ySurface, chunkZ * 32 + dz);
				}
				if (startPos.Y <= 0 || !BlockSchematicStructure.SatisfiesMinSpawnDistance(struc.MinSpawnDistance, startPos, spawnPos))
				{
					continue;
				}
				if (locationCode != null)
				{
					location = GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16);
					Dictionary<string, int> schematicsSpawned = location.SchematicsSpawned;
					if (schematicsSpawned != null && schematicsSpawned.TryGetValue(struc.Group, out var spawnedSchematics2) && spawnedSchematics2 >= struc.StoryLocationMaxAmount)
					{
						continue;
					}
				}
				if (!struc.TryGenerate(worldgenBlockAccessor, api.World, startPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, locationCode))
				{
					continue;
				}
				if (locationCode != null && location != null)
				{
					Dictionary<string, int> schematicsSpawned2 = location.SchematicsSpawned;
					if (schematicsSpawned2 != null && schematicsSpawned2.TryGetValue(struc.Group, out var spawnedSchematics))
					{
						location.SchematicsSpawned[struc.Group] = spawnedSchematics + 1;
					}
					else
					{
						StoryStructureLocation storyStructureLocation = location;
						if (storyStructureLocation.SchematicsSpawned == null)
						{
							storyStructureLocation.SchematicsSpawned = new Dictionary<string, int>();
						}
						location.SchematicsSpawned[struc.Group] = 1;
					}
				}
				Cuboidi loc = struc.LastPlacedSchematicLocation;
				string code = struc.Code + ((struc.LastPlacedSchematic == null) ? "" : ("/" + struc.LastPlacedSchematic.FromFileName));
				region.AddGeneratedStructure(new GeneratedStructure
				{
					Code = code,
					Group = struc.Group,
					Location = loc.Clone(),
					SuppressTreesAndShrubs = struc.SuppressTrees,
					SuppressRivulets = struc.SuppressWaterfalls
				});
				if (struc.BuildProtected)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { loc.Clone() },
						Description = struc.BuildProtectionDesc,
						ProtectionLevel = 10,
						LastKnownOwnerName = struc.BuildProtectionName,
						AllowUseEveryone = true
					});
				}
				toGenerate--;
			}
		}
	}

	public void TryGenVillages(IMapRegion region, int chunkX, int chunkZ, bool postPass, ITreeAttribute chunkGenParams = null)
	{
		strucRand.InitPositionSeed(chunkX, chunkZ);
		for (int i = 0; i < vcfg.VillageTypes.Length; i++)
		{
			WorldGenVillage struc = vcfg.VillageTypes[i];
			if (struc.PostPass == postPass)
			{
				float chance = struc.Chance * vcfg.ChanceMultiplier;
				while (chance-- > strucRand.NextFloat())
				{
					GenVillage(worldgenBlockAccessor, region, struc, chunkX, chunkZ);
				}
			}
		}
	}

	public bool GenVillage(IBlockAccessor blockAccessor, IMapRegion region, WorldGenVillage struc, int chunkX, int chunkZ)
	{
		BlockPos pos = new BlockPos();
		int dx = 16;
		int dz = 16;
		int ySurface = heightmap[dz * 32 + dx];
		if (ySurface <= 0 || ySurface >= worldheight - 15)
		{
			return false;
		}
		pos.Set(chunkX * 32 + dx, ySurface, chunkZ * 32 + dz);
		return struc.TryGenerate(blockAccessor, api.World, pos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, delegate(Cuboidi loc, BlockSchematicStructure schematic)
		{
			string code = struc.Code + ((schematic == null) ? "" : ("/" + schematic.FromFileName));
			region.AddGeneratedStructure(new GeneratedStructure
			{
				Code = code,
				Group = struc.Group,
				Location = loc.Clone()
			});
			if (struc.BuildProtected)
			{
				api.World.Claims.Add(new LandClaim
				{
					Areas = new List<Cuboidi> { loc.Clone() },
					Description = struc.BuildProtectionDesc,
					ProtectionLevel = 10,
					LastKnownOwnerName = struc.BuildProtectionName,
					AllowUseEveryone = true
				});
			}
		}, spawnPos);
	}
}
