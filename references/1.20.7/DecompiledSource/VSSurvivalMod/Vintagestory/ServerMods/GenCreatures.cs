using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenCreatures : ModStdWorldGen
{
	private ICoreServerAPI api;

	private Random rnd;

	private int worldheight;

	private IWorldGenBlockAccessor wgenBlockAccessor;

	private Dictionary<EntityProperties, EntityProperties[]> entityTypeGroups = new Dictionary<EntityProperties, EntityProperties[]>();

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private int forestUpLeft;

	private int forestUpRight;

	private int forestBotLeft;

	private int forestBotRight;

	private int shrubsUpLeft;

	private int shrubsUpRight;

	private int shrubsBotLeft;

	private int shrubsBotRight;

	private List<SpawnOppurtunity> spawnPositions = new List<SpawnOppurtunity>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.1;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.PreDone, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		wgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void initWorldGen()
	{
		LoadGlobalConfig(api);
		rnd = new Random(api.WorldManager.Seed - 18722);
		worldheight = api.WorldManager.MapSizeY;
		Dictionary<AssetLocation, EntityProperties> entityTypesByCode = new Dictionary<AssetLocation, EntityProperties>();
		for (int j = 0; j < api.World.EntityTypes.Count; j++)
		{
			entityTypesByCode[api.World.EntityTypes[j].Code] = api.World.EntityTypes[j];
		}
		Dictionary<AssetLocation, Block[]> searchCache = new Dictionary<AssetLocation, Block[]>();
		for (int i = 0; i < api.World.EntityTypes.Count; i++)
		{
			EntityProperties type = api.World.EntityTypes[i];
			WorldGenSpawnConditions conds = type.Server?.SpawnConditions?.Worldgen;
			if (conds == null)
			{
				continue;
			}
			List<EntityProperties> grouptypes = new List<EntityProperties>();
			grouptypes.Add(type);
			conds.Initialise(api.World, type.Code.ToShortString(), searchCache);
			AssetLocation[] companions = conds.Companions;
			if (companions == null)
			{
				continue;
			}
			for (int k = 0; k < companions.Length; k++)
			{
				if (entityTypesByCode.TryGetValue(companions[k], out var cptype))
				{
					grouptypes.Add(cptype);
				}
			}
			entityTypeGroups[type] = grouptypes.ToArray();
		}
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		if (GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16, ModStdWorldGen.SkipCreaturesgHashCode) != null)
		{
			return;
		}
		wgenBlockAccessor.BeginColumn();
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		ushort[] heightMap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		int rlX = chunkX % regionChunkSize;
		int rlZ = chunkZ % regionChunkSize;
		float facC = (float)climateMap.InnerSize / (float)regionChunkSize;
		climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC));
		climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC));
		climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC + facC));
		climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC + facC));
		IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
		float facF = (float)forestMap.InnerSize / (float)regionChunkSize;
		forestUpLeft = forestMap.GetUnpaddedInt((int)((float)rlX * facF), (int)((float)rlZ * facF));
		forestUpRight = forestMap.GetUnpaddedInt((int)((float)rlX * facF + facF), (int)((float)rlZ * facF));
		forestBotLeft = forestMap.GetUnpaddedInt((int)((float)rlX * facF), (int)((float)rlZ * facF + facF));
		forestBotRight = forestMap.GetUnpaddedInt((int)((float)rlX * facF + facF), (int)((float)rlZ * facF + facF));
		IntDataMap2D shrubMap = chunks[0].MapChunk.MapRegion.ShrubMap;
		float facS = (float)shrubMap.InnerSize / (float)regionChunkSize;
		shrubsUpLeft = shrubMap.GetUnpaddedInt((int)((float)rlX * facS), (int)((float)rlZ * facS));
		shrubsUpRight = shrubMap.GetUnpaddedInt((int)((float)rlX * facS + facS), (int)((float)rlZ * facS));
		shrubsBotLeft = shrubMap.GetUnpaddedInt((int)((float)rlX * facS), (int)((float)rlZ * facS + facS));
		shrubsBotRight = shrubMap.GetUnpaddedInt((int)((float)rlX * facS + facS), (int)((float)rlZ * facS + facS));
		Vec3d posAsVec = new Vec3d();
		BlockPos pos = new BlockPos();
		foreach (KeyValuePair<EntityProperties, EntityProperties[]> val in entityTypeGroups)
		{
			EntityProperties entitytype = val.Key;
			float tries = entitytype.Server.SpawnConditions.Worldgen.TriesPerChunk.nextFloat(1f, rnd);
			RuntimeSpawnConditions scRuntime = entitytype.Server.SpawnConditions.Runtime;
			if (scRuntime == null || scRuntime.Group != "hostile")
			{
				tries *= GlobalConfig.neutralCreatureSpawnMultiplier;
			}
			while ((double)tries-- > rnd.NextDouble())
			{
				int dx = rnd.Next(32);
				int dz = rnd.Next(32);
				pos.Set(chunkX * 32 + dx, 0, chunkZ * 32 + dz);
				pos.Y = (entitytype.Server.SpawnConditions.Worldgen.TryOnlySurface ? (heightMap[dz * 32 + dx] + 1) : rnd.Next(worldheight));
				posAsVec.Set((double)pos.X + 0.5, (double)pos.Y + 0.005, (double)pos.Z + 0.5);
				TrySpawnGroupAt(pos, posAsVec, entitytype, val.Value);
			}
		}
	}

	private void TrySpawnGroupAt(BlockPos origin, Vec3d posAsVec, EntityProperties entityType, EntityProperties[] grouptypes)
	{
		BlockPos pos = origin.Copy();
		int spawned = 0;
		WorldGenSpawnConditions sc = entityType.Server.SpawnConditions.Worldgen;
		spawnPositions.Clear();
		int nextGroupSize = 0;
		int tries = 10;
		while (nextGroupSize <= 0 && tries-- > 0)
		{
			float val = sc.HerdSize.nextFloat();
			nextGroupSize = (int)val + (((double)(val - (float)(int)val) > rnd.NextDouble()) ? 1 : 0);
		}
		for (int i = 0; i < nextGroupSize * 4 + 5; i++)
		{
			if (spawned >= nextGroupSize)
			{
				break;
			}
			EntityProperties typeToSpawn = entityType;
			double dominantChance = ((i == 0) ? 0.8 : Math.Min(0.2, 1f / (float)grouptypes.Length));
			if (grouptypes.Length > 1 && rnd.NextDouble() > dominantChance)
			{
				typeToSpawn = grouptypes[1 + rnd.Next(grouptypes.Length - 1)];
			}
			IBlockAccessor blockAccessor2;
			if (wgenBlockAccessor.GetChunkAtBlockPos(pos) != null)
			{
				IBlockAccessor blockAccessor = wgenBlockAccessor;
				blockAccessor2 = blockAccessor;
			}
			else
			{
				blockAccessor2 = api.World.BlockAccessor;
			}
			IBlockAccessor blockAccesssor = blockAccessor2;
			IMapChunk mapchunk = blockAccesssor.GetMapChunkAtBlockPos(pos);
			if (mapchunk != null)
			{
				if (sc.TryOnlySurface)
				{
					ushort[] heightMap = mapchunk.WorldGenTerrainHeightMap;
					pos.Y = heightMap[pos.Z % 32 * 32 + pos.X % 32] + 1;
				}
				if (CanSpawnAtPosition(blockAccesssor, typeToSpawn, pos, sc))
				{
					posAsVec.Set((double)pos.X + 0.5, (double)pos.Y + 0.005, (double)pos.Z + 0.5);
					float xRel = (float)(posAsVec.X % 32.0) / 32f;
					float zRel = (float)(posAsVec.Z % 32.0) / 32f;
					int num = GameMath.BiLerpRgbColor(xRel, zRel, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
					float temp = Climate.GetScaledAdjustedTemperatureFloat((num >> 16) & 0xFF, (int)posAsVec.Y - TerraGenConfig.seaLevel);
					float rain = (float)((num >> 8) & 0xFF) / 255f;
					float forestDensity = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, xRel, zRel) / 255f;
					float shrubDensity = GameMath.BiLerp(shrubsUpLeft, shrubsUpRight, shrubsBotLeft, shrubsBotRight, xRel, zRel) / 255f;
					if (CanSpawnAtConditions(blockAccesssor, typeToSpawn, pos, posAsVec, sc, rain, temp, forestDensity, shrubDensity))
					{
						spawnPositions.Add(new SpawnOppurtunity
						{
							ForType = typeToSpawn,
							Pos = posAsVec.Clone()
						});
						spawned++;
					}
				}
			}
			pos.X = origin.X + (rnd.Next(11) - 5 + (rnd.Next(11) - 5)) / 2;
			pos.Z = origin.Z + (rnd.Next(11) - 5 + (rnd.Next(11) - 5)) / 2;
		}
		if (spawnPositions.Count < nextGroupSize)
		{
			return;
		}
		long herdId = api.WorldManager.GetNextUniqueId();
		foreach (SpawnOppurtunity so in spawnPositions)
		{
			Entity ent = CreateEntity(so.ForType, so.Pos);
			if (ent is EntityAgent)
			{
				(ent as EntityAgent).HerdId = herdId;
			}
			if (api.Event.TriggerTrySpawnEntity(wgenBlockAccessor, ref so.ForType, so.Pos, herdId))
			{
				if (wgenBlockAccessor.GetChunkAtBlockPos(pos) == null)
				{
					api.World.SpawnEntity(ent);
				}
				else
				{
					wgenBlockAccessor.AddEntity(ent);
				}
			}
		}
	}

	private Entity CreateEntity(EntityProperties entityType, Vec3d spawnPosition)
	{
		Entity entity = api.ClassRegistry.CreateEntity(entityType);
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)rnd.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "worldgen");
		return entity;
	}

	private bool CanSpawnAtPosition(IBlockAccessor blockAccessor, EntityProperties type, BlockPos pos, BaseSpawnConditions sc)
	{
		if (!blockAccessor.IsValidPos(pos))
		{
			return false;
		}
		Block block = blockAccessor.GetBlock(pos);
		if (!sc.CanSpawnInside(block))
		{
			return false;
		}
		pos.Y--;
		if (!blockAccessor.GetBlock(pos).CanCreatureSpawnOn(blockAccessor, pos, type, sc))
		{
			pos.Y++;
			return false;
		}
		pos.Y++;
		return true;
	}

	private bool CanSpawnAtConditions(IBlockAccessor blockAccessor, EntityProperties type, BlockPos pos, Vec3d posAsVec, BaseSpawnConditions sc, float rain, float temp, float forestDensity, float shrubsDensity)
	{
		float? lightLevel = blockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight);
		if (!lightLevel.HasValue)
		{
			return false;
		}
		if ((float)sc.MinLightLevel > lightLevel || (float)sc.MaxLightLevel < lightLevel)
		{
			return false;
		}
		if (sc.MinTemp > temp || sc.MaxTemp < temp)
		{
			return false;
		}
		if (sc.MinRain > rain || sc.MaxRain < rain)
		{
			return false;
		}
		if (sc.MinForest > forestDensity || sc.MaxForest < forestDensity)
		{
			return false;
		}
		if (sc.MinShrubs > shrubsDensity || sc.MaxShrubs < shrubsDensity)
		{
			return false;
		}
		if (sc.MinForestOrShrubs > Math.Max(forestDensity, shrubsDensity))
		{
			return false;
		}
		double yRel = ((pos.Y > TerraGenConfig.seaLevel) ? (1.0 + ((double)pos.Y - (double)TerraGenConfig.seaLevel) / (double)(api.World.BlockAccessor.MapSizeY - TerraGenConfig.seaLevel)) : ((double)pos.Y / (double)TerraGenConfig.seaLevel));
		if ((double)sc.MinY > yRel || (double)sc.MaxY < yRel)
		{
			return false;
		}
		Cuboidf collisionBox = type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
		return !IsColliding(collisionBox, posAsVec);
	}

	public bool IsColliding(Cuboidf entityBoxRel, Vec3d pos)
	{
		BlockPos blockPos = new BlockPos();
		Cuboidd entityCuboid = entityBoxRel.ToDouble().Translate(pos);
		Vec3d blockPosAsVec = new Vec3d();
		int minX = (int)((double)entityBoxRel.X1 + pos.X);
		int num = (int)((double)entityBoxRel.Y1 + pos.Y);
		int minZ = (int)((double)entityBoxRel.Z1 + pos.Z);
		int maxX = (int)Math.Ceiling((double)entityBoxRel.X2 + pos.X);
		int maxY = (int)Math.Ceiling((double)entityBoxRel.Y2 + pos.Y);
		int maxZ = (int)Math.Ceiling((double)entityBoxRel.Z2 + pos.Z);
		for (int y = num; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				for (int z = minZ; z <= maxZ; z++)
				{
					IBlockAccessor blockAccess = wgenBlockAccessor;
					IWorldChunk chunk = wgenBlockAccessor.GetChunkAtBlockPos(x, y, z);
					if (chunk == null)
					{
						chunk = api.World.BlockAccessor.GetChunkAtBlockPos(x, y, z);
						blockAccess = api.World.BlockAccessor;
					}
					if (chunk == null)
					{
						return true;
					}
					int index = (y % 32 * 32 + z % 32) * 32 + x % 32;
					Block block = api.World.Blocks[chunk.UnpackAndReadBlock(index, 0)];
					blockPos.Set(x, y, z);
					blockPosAsVec.Set(x, y, z);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccess, blockPos);
					int i = 0;
					while (collisionBoxes != null && i < collisionBoxes.Length)
					{
						Cuboidf collBox = collisionBoxes[i];
						if (collBox != null && entityCuboid.Intersects(collBox, blockPosAsVec))
						{
							return true;
						}
						i++;
					}
				}
			}
		}
		return false;
	}
}
