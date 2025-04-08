using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class ServerSystemEntitySpawner : ServerSystem
{
	private const int yWiggle = 3;

	private const int xzWiggle = 5;

	private const int chunksize = 32;

	private const int chunkRange = 3;

	private const float globalMultiplier = 1f;

	private int GraceTimerDayNotify = -1;

	private double GraceTimeUntilTotalDays = 5.0;

	private List<SpawnArea> spawnAreas = new List<SpawnArea>();

	private List<SpawnState> spawnStates = new List<SpawnState>();

	private HashSet<Vec2i> chunkColumnCoordsTmp = new HashSet<Vec2i>();

	private CollisionTester collisionTester = new CollisionTester();

	private Dictionary<AssetLocation, EntityProperties> entityTypesByCode = new Dictionary<AssetLocation, EntityProperties>();

	private Random rand = new Random();

	private ushort[] heightMap;

	private ICachingBlockAccessor cachingBlockAccessor;

	private float slowaccum;

	private bool first = true;

	private int SeaLevel;

	private int SkyHeight;

	private Vec3i spawnPosition = new Vec3i();

	private int[] shuffledY = new int[7] { -3, -2, -1, 0, 1, 2, 3 };

	private int surfaceMapTryID;

	private bool errorLoggedThisTick;

	private readonly BlockPos tmpPos = new BlockPos();

	public ServerSystemEntitySpawner(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(TrySpawnMobs, 500);
	}

	public override void OnBeginModsAndConfigReady()
	{
		List<EntityProperties> entityTypes = server.EntityTypes;
		for (int i = 0; i < entityTypes.Count; i++)
		{
			EntityProperties entityType = entityTypes[i];
			entityTypesByCode[entityType.Code] = entityType;
		}
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		base.OnBeginGameReady(savegame);
		cachingBlockAccessor = server.GetCachingBlockAccessor(synchronize: false, relight: false);
		if (savegame.IsNewWorld || !savegame.ModData.ContainsKey("graceTimeUntilTotalDays"))
		{
			string @string = savegame.WorldConfiguration.GetString("graceTimer", "5");
			GraceTimeUntilTotalDays = 5.0;
			double.TryParse(@string, out GraceTimeUntilTotalDays);
			if (!savegame.IsNewWorld && !savegame.ModData.ContainsKey("graceTimeUntilTotalDays"))
			{
				int dayspm = Math.Max(1, savegame.WorldConfiguration.GetAsInt("daysPerMonth", 12));
				double daysStart = 1f / 3f + (float)(dayspm * 4);
				GraceTimeUntilTotalDays += daysStart;
			}
			else
			{
				GraceTimeUntilTotalDays += server.Calendar.TotalDays;
			}
			savegame.ModData["graceTimeUntilTotalDays"] = SerializerUtil.Serialize(GraceTimeUntilTotalDays);
		}
		else
		{
			GraceTimeUntilTotalDays = SerializerUtil.Deserialize<double>(savegame.ModData["graceTimeUntilTotalDays"]);
		}
		Dictionary<AssetLocation, Block[]> searchCache = new Dictionary<AssetLocation, Block[]>();
		foreach (EntityProperties type in server.EntityTypes)
		{
			(type.Server.SpawnConditions?.Runtime)?.Initialise(server, type.Code.ToShortString(), searchCache);
		}
	}

	public override void Dispose()
	{
		cachingBlockAccessor?.Dispose();
		cachingBlockAccessor = null;
	}

	private void TrySpawnMobs(float dt)
	{
		if (!server.SaveGameData.EntitySpawning || server.Clients.Count == 0)
		{
			return;
		}
		ServerMain.FrameProfiler.Enter("entityspawner");
		slowaccum += dt;
		int day = (int)server.Calendar.TotalDays;
		double daysLeft = GraceTimeUntilTotalDays - server.Calendar.TotalDays + 0.25;
		if (GraceTimerDayNotify != day && daysLeft >= 0.0)
		{
			GraceTimerDayNotify = day;
			if ((int)daysLeft > 1)
			{
				server.SendMessageToGeneral(Lang.Get("server-xdaysleft", (int)daysLeft), EnumChatType.Notification);
			}
			if ((int)daysLeft == 1)
			{
				server.SendMessageToGeneral(Lang.Get("server-1dayleft"), EnumChatType.Notification);
			}
			if ((int)daysLeft == 0)
			{
				server.SendMessageToGeneral(Lang.Get("server-monsterbegins"), EnumChatType.Notification);
			}
		}
		ReloadSpawnStates(first);
		if (first || slowaccum > 10f)
		{
			LoadViableSpawnAreas();
			slowaccum = 0f;
			first = false;
		}
		cachingBlockAccessor.Begin();
		SeaLevel = server.SeaLevel;
		SkyHeight = server.WorldMap.MapSizeY - SeaLevel;
		errorLoggedThisTick = false;
		ServerMain.FrameProfiler.Enter("spawn-attempts");
		for (int i = 0; i < spawnAreas.Count; i++)
		{
			SpawnArea spawnArea = spawnAreas[i];
			for (int j = 0; j < spawnArea.ChunkColumnCoords.Length; j++)
			{
				Vec2i coord = spawnArea.ChunkColumnCoords[j];
				TrySpawnSomethingAt(coord.X, spawnArea.chunkY, coord.Y, spawnArea.spawnCounts);
			}
		}
		ServerMain.FrameProfiler.Leave();
		ServerMain.FrameProfiler.Leave();
	}

	private void TrySpawnSomethingAt(int baseX, int baseY, int baseZ, Dictionary<AssetLocation, int> spawnCounts)
	{
		heightMap = server.WorldMap.GetMapChunk(baseX, baseZ)?.WorldGenTerrainHeightMap;
		if (heightMap == null)
		{
			return;
		}
		bool entireColumnLoaded = true;
		IWorldChunk[] chunkCol = new IWorldChunk[server.WorldMap.ChunkMapSizeY];
		for (int cy2 = 0; cy2 < chunkCol.Length; cy2++)
		{
			IWorldChunk chunk2 = server.WorldMap.GetChunk(baseX, cy2, baseZ);
			if (chunk2 == null)
			{
				entireColumnLoaded = false;
				continue;
			}
			chunk2.Unpack_ReadOnly();
			chunk2.AcquireBlockReadLock();
			chunkCol[cy2] = chunk2;
		}
		try
		{
			if (entireColumnLoaded)
			{
				TrySpawnSomethingAt(baseX, baseY, baseZ, spawnCounts, chunkCol);
			}
		}
		finally
		{
			foreach (IWorldChunk chunk in chunkCol)
			{
				if (chunk != null)
				{
					chunk.ReleaseBlockReadLock();
					continue;
				}
				break;
			}
		}
	}

	private void TrySpawnSomethingAt(int baseX, int baseY, int baseZ, Dictionary<AssetLocation, int> spawnCounts, IWorldChunk[] chunkCol)
	{
		Vec3i spawnPosition = this.spawnPosition;
		int mapsizey = server.WorldMap.MapSizeY;
		List<SpawnOppurtunity> spawnPositions = new List<SpawnOppurtunity>();
		surfaceMapTryID++;
		shuffledY.Shuffle(rand);
		for (int yIndex = 0; yIndex < shuffledY.Length; yIndex++)
		{
			int startY = (baseY + shuffledY[yIndex]) * 32 + rand.Next(32);
			if (startY <= -3 || startY >= mapsizey + 3)
			{
				continue;
			}
			int startX;
			int startZ;
			spawnPosition.Set(startX = baseX * 32 + rand.Next(32), startY, startZ = baseZ * 32 + rand.Next(32));
			foreach (SpawnState spawnState in spawnStates)
			{
				RuntimeSpawnConditions sc = spawnState.ForType.Server.SpawnConditions.Runtime;
				if (spawnState.SpawnableAmountGlobal <= 0)
				{
					continue;
				}
				spawnCounts.TryGetValue(spawnState.ForType.Code, out var areaSpawned);
				if (areaSpawned > spawnState.SpawnCapScaledPerPlayer)
				{
					continue;
				}
				if (!sc.TryOnlySurface)
				{
					double y = startY + 3;
					double yRel2 = ((y > (double)SeaLevel) ? (1.0 + (y - (double)SeaLevel) / (double)SkyHeight) : (y / (double)SeaLevel));
					if ((double)sc.MinY > yRel2)
					{
						continue;
					}
					y -= 6.0;
					yRel2 = ((y > (double)SeaLevel) ? (1.0 + (y - (double)SeaLevel) / (double)SkyHeight) : (y / (double)SeaLevel));
					if ((double)sc.MaxY < yRel2)
					{
						continue;
					}
				}
				int tries = 10;
				while (spawnState.NextGroupSize <= 0 && tries-- > 0)
				{
					float val = sc.HerdSize.nextFloat();
					spawnState.NextGroupSize = (int)val + (((double)(val - (float)(int)val) > rand.NextDouble()) ? 1 : 0);
				}
				spawnPositions.Clear();
				int qSelfAndCompanions = spawnState.SelfAndCompanionProps.Length;
				EntityProperties typeToSpawn = spawnState.SelfAndCompanionProps[0];
				tries = spawnState.NextGroupSize * 4 + 5;
				for (int i = 0; i < tries && spawnPositions.Count < spawnState.NextGroupSize; i++)
				{
					spawnPosition.X = randomWithinSameChunk(startX);
					spawnPosition.Z = randomWithinSameChunk(startZ);
					if (sc.TryOnlySurface)
					{
						int mapIndex = spawnPosition.Z % 32 * 32 + spawnPosition.X % 32;
						if (spawnState.surfaceMap == null)
						{
							spawnState.surfaceMap = new int[1024];
						}
						if (spawnState.surfaceMap[mapIndex] == surfaceMapTryID)
						{
							continue;
						}
						spawnState.surfaceMap[mapIndex] = surfaceMapTryID;
						spawnPosition.Y = heightMap[mapIndex] + 1;
					}
					else
					{
						spawnPosition.Y = Math.Max(1, startY + rand.Next(7) - 3);
					}
					if (spawnPosition.Y < 1 || spawnPosition.Y >= server.WorldMap.MapSizeY)
					{
						i++;
						continue;
					}
					double yRel = ((spawnPosition.Y > SeaLevel) ? (1.0 + (double)(spawnPosition.Y - SeaLevel) / (double)SkyHeight) : ((double)spawnPosition.Y / (double)SeaLevel));
					if ((double)sc.MinY > yRel || (double)sc.MaxY < yRel)
					{
						i++;
						continue;
					}
					if (spawnPositions.Count > 0 && qSelfAndCompanions > 1)
					{
						int rnd = 1 + rand.Next(qSelfAndCompanions - 1);
						typeToSpawn = spawnState.SelfAndCompanionProps[rnd];
					}
					Vec3d canSpawnPos = CanSpawnAt(typeToSpawn, spawnPosition, sc, chunkCol);
					if (canSpawnPos != null)
					{
						spawnPositions.Add(new SpawnOppurtunity
						{
							ForType = typeToSpawn,
							Pos = canSpawnPos
						});
					}
					if (spawnPositions.Count == 0)
					{
						i++;
					}
				}
				if (spawnPositions.Count >= spawnState.NextGroupSize)
				{
					long herdid = server.GetNextHerdId();
					int quantityTospawn = spawnState.NextGroupSize;
					if (server.SpawnDebug)
					{
						ServerMain.Logger.Notification("Spawn {0}x {1} @{2}/{3}/{4}", spawnPositions.Count, spawnPositions[0].ForType.Code, (int)spawnPositions[0].Pos.X, (int)spawnPositions[0].Pos.Y, (int)spawnPositions[0].Pos.Z);
					}
					ServerMain.FrameProfiler.Mark(spawnState.profilerName);
					foreach (SpawnOppurtunity so in spawnPositions)
					{
						if (quantityTospawn-- <= 0)
						{
							break;
						}
						EntityProperties props = so.ForType;
						if (server.EventManager.TriggerTrySpawnEntity(server.blockAccessor, ref props, so.Pos, herdid))
						{
							DoSpawn(props, so.Pos, herdid);
							spawnState.SpawnableAmountGlobal--;
						}
					}
					spawnState.NextGroupSize = -1;
				}
				if (ServerMain.FrameProfiler.Enabled)
				{
					ServerMain.FrameProfiler.Mark(spawnState.profilerName);
				}
			}
		}
	}

	private int randomWithinSameChunk(int x)
	{
		return (x & -32) + (x + rand.Next(11) - 5 + 32) % 32;
	}

	private void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdid)
	{
		Entity entity = server.Api.ClassRegistry.CreateEntity(entityType);
		if (entity is EntityAgent agent)
		{
			agent.HerdId = herdid;
		}
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "entityspawner");
		server.DelayedSpawnQueue.Enqueue(entity);
	}

	private Vec3d CanSpawnAt(EntityProperties type, Vec3i spawnPosition, RuntimeSpawnConditions sc, IWorldChunk[] chunkCol)
	{
		if (spawnPosition.Y <= 0 || spawnPosition.Y >= server.WorldMap.MapSizeY)
		{
			return null;
		}
		try
		{
			tmpPos.Set(spawnPosition);
			ClimateCondition climate;
			Block block;
			if (sc.TryOnlySurface)
			{
				climate = GetSuitableClimateTemperatureRainfall(sc);
				if (climate == null)
				{
					return null;
				}
				block = chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(server.WorldMap.World, tmpPos);
				if (!sc.CanSpawnInside(block))
				{
					return null;
				}
				tmpPos.Y--;
				if (!chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(server.WorldMap.World, tmpPos).CanCreatureSpawnOn(cachingBlockAccessor, tmpPos, type, sc))
				{
					return null;
				}
			}
			else
			{
				block = chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(server.WorldMap.World, tmpPos);
				bool canspawn = false;
				for (int i = 0; i < 5; i++)
				{
					if (--tmpPos.Y < 0)
					{
						break;
					}
					Block belowBlock = chunkCol[tmpPos.Y / 32].GetLocalBlockAtBlockPos_LockFree(server.WorldMap.World, tmpPos);
					if (sc.CanSpawnInside(block) && belowBlock.CanCreatureSpawnOn(cachingBlockAccessor, tmpPos, type, sc))
					{
						canspawn = true;
						break;
					}
					spawnPosition.Y--;
					block = belowBlock;
				}
				if (!canspawn)
				{
					return null;
				}
				climate = GetSuitableClimateTemperatureRainfall(sc);
				if (climate == null)
				{
					return null;
				}
			}
			tmpPos.Y++;
			IMapRegion mapregion = server.WorldMap.GetMapRegion(tmpPos);
			server.WorldMap.AddWorldGenForestShrub(climate, mapregion, tmpPos);
			if (sc.MinForest > climate.ForestDensity || sc.MaxForest < climate.ForestDensity)
			{
				return null;
			}
			if (sc.MinShrubs > climate.ShrubDensity || sc.MaxShrubs < climate.ShrubDensity)
			{
				return null;
			}
			if (sc.MinForestOrShrubs > Math.Max(climate.ForestDensity, climate.ShrubDensity))
			{
				return null;
			}
			double yOffset = 1E-07;
			Cuboidf[] insideBlockBoxes = block.GetCollisionBoxes(server.BlockAccessor, tmpPos);
			if (insideBlockBoxes != null && insideBlockBoxes.Length != 0)
			{
				yOffset += (double)(insideBlockBoxes[0].MaxY % 1f);
			}
			Vec3d spawnPosition3d = new Vec3d((double)spawnPosition.X + 0.5, (double)spawnPosition.Y + yOffset, (double)spawnPosition.Z + 0.5);
			Cuboidf collisionBox = type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
			if (collisionTester.IsColliding(server.BlockAccessor, collisionBox, spawnPosition3d, alsoCheckTouch: false))
			{
				return null;
			}
			IPlayer plr = server.NearestPlayer(spawnPosition3d.X, spawnPosition3d.Y, spawnPosition3d.Z);
			if (plr?.Entity != null && !plr.Entity.CanSpawnNearby(type, spawnPosition3d, sc))
			{
				return null;
			}
			return spawnPosition3d;
		}
		catch (Exception e)
		{
			if (!errorLoggedThisTick)
			{
				errorLoggedThisTick = true;
				server.World.Logger.Warning("Error when testing to spawn entity {0} at position {1}, can report to dev team but otherwise should do no harm.", type.Code.ToShortString(), spawnPosition);
				server.World.Logger.Error(e);
			}
			return null;
		}
	}

	private ClimateCondition GetSuitableClimateTemperatureRainfall(RuntimeSpawnConditions sc)
	{
		ClimateCondition climate = server.WorldMap.getWorldGenClimateAt(tmpPos, temperatureRainfallOnly: true);
		if (climate == null)
		{
			return null;
		}
		if (sc.ClimateValueMode != 0)
		{
			server.WorldMap.GetClimateAt(tmpPos, climate, sc.ClimateValueMode, server.Calendar.TotalDays);
		}
		if (sc.MinTemp > climate.Temperature || sc.MaxTemp < climate.Temperature)
		{
			return null;
		}
		if (sc.MinRain > climate.Rainfall || sc.MaxRain < climate.Rainfall)
		{
			return null;
		}
		return climate;
	}

	private void LoadViableSpawnAreas()
	{
		spawnAreas.Clear();
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (!client.IsPlayingClient || client.Entityplayer == null)
			{
				continue;
			}
			int chunkX = (int)client.Entityplayer.Pos.X / 32;
			int chunkY = (int)client.Entityplayer.Pos.Y / 32;
			int chunkZ = (int)client.Entityplayer.Pos.Z / 32;
			SpawnArea area = new SpawnArea();
			area.chunkY = chunkY;
			chunkColumnCoordsTmp.Clear();
			for (int dx = -3; dx <= 3; dx++)
			{
				for (int dz = -3; dz <= 3; dz++)
				{
					Vec3i vec = new Vec3i(chunkX + dx, chunkY, chunkZ + dz);
					bool columnLoaded = false;
					for (int dy = Math.Max(-3, -chunkY); dy <= 3; dy++)
					{
						IWorldChunk chunk = server.WorldMap.GetChunk(vec.X, chunkY + dy, vec.Z);
						if (chunk == null)
						{
							continue;
						}
						columnLoaded = true;
						if (chunk.Entities == null)
						{
							continue;
						}
						for (int i = 0; i < chunk.Entities.Length; i++)
						{
							Entity e = chunk.Entities[i];
							int cnt = 0;
							if (e != null)
							{
								area.spawnCounts.TryGetValue(e.Code, out cnt);
							}
							else if (i >= chunk.EntitiesCount)
							{
								break;
							}
							area.spawnCounts[e.Code] = cnt + 1;
						}
					}
					if (columnLoaded)
					{
						chunkColumnCoordsTmp.Add(vec.XZ);
					}
				}
			}
			if (chunkColumnCoordsTmp.Count > 0)
			{
				area.ChunkColumnCoords = chunkColumnCoordsTmp.ToArray();
				area.ChunkColumnCoords.Shuffle(rand);
				spawnAreas.Add(area);
			}
		}
	}

	private void ReloadSpawnStates(bool isInitialLoad)
	{
		double daysLeft = GraceTimeUntilTotalDays - server.Calendar.TotalDays;
		Dictionary<AssetLocation, int> quantityLoaded = new Dictionary<AssetLocation, int>();
		foreach (Entity entity in server.LoadedEntities.Values)
		{
			if (!(entity.Code == null))
			{
				quantityLoaded.TryGetValue(entity.Code, out var beforeQuantity);
				quantityLoaded[entity.Code] = beforeQuantity + 1;
				QuantityByGroup maxqgrp = entity.Properties.Server.SpawnConditions?.Runtime?.MaxQuantityByGroup;
				if (maxqgrp != null && WildcardUtil.Match(maxqgrp.Code, entity.Code))
				{
					quantityLoaded.TryGetValue(maxqgrp.Code, out beforeQuantity);
					quantityLoaded[maxqgrp.Code] = beforeQuantity + 1;
				}
			}
		}
		spawnStates.Clear();
		Random rand = server.rand.Value;
		foreach (EntityProperties type in server.EntityTypes)
		{
			RuntimeSpawnConditions conds = type.Server.SpawnConditions?.Runtime;
			if (conds == null || conds.MaxQuantity == 0 || (daysLeft > 0.0 && conds.Group == "hostile") || rand.NextDouble() >= 1.0 * conds.Chance)
			{
				continue;
			}
			quantityLoaded.TryGetValue(type.Code, out var qNow);
			float spawnCapMul = 1f + Math.Max(0f, (float)(server.AllOnlinePlayers.Length - 1) * server.Config.SpawnCapPlayerScaling * conds.SpawnCapPlayerScaling);
			int spawnableAmount = (int)((float)conds.MaxQuantity * spawnCapMul - (float)qNow);
			if (conds.MaxQuantityByGroup != null)
			{
				quantityLoaded.TryGetValue(conds.MaxQuantityByGroup.Code, out var qNowGroup);
				spawnableAmount = Math.Min(spawnableAmount, (int)((float)conds.MaxQuantityByGroup.MaxQuantity * spawnCapMul) - qNowGroup);
			}
			if (spawnableAmount <= 0)
			{
				continue;
			}
			bool num = conds.Companions != null && conds.Companions.Length != 0;
			List<EntityProperties> selfAndCompanionsProps = new List<EntityProperties> { type };
			if (num)
			{
				for (int i = 0; i < conds.Companions.Length; i++)
				{
					if (entityTypesByCode.TryGetValue(conds.Companions[i], out var companionType))
					{
						selfAndCompanionsProps.Add(companionType);
					}
					else if (isInitialLoad)
					{
						ServerMain.Logger.Warning("Entity with code {0} has defined a companion spawn {1}, but no such entity type found.", type.Code, conds.Companions[i]);
					}
				}
			}
			spawnStates.Add(new SpawnState
			{
				ForType = type,
				profilerName = "testspawn " + type.Code,
				SpawnableAmountGlobal = spawnableAmount,
				SpawnCapScaledPerPlayer = (int)((float)conds.MaxQuantity * spawnCapMul / (float)server.AllOnlinePlayers.Length),
				SelfAndCompanionProps = selfAndCompanionsProps.ToArray()
			});
		}
	}
}
