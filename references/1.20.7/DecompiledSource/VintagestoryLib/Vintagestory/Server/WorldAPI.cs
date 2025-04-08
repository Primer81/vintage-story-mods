using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class WorldAPI : ServerAPIComponentBase, IWorldManagerAPI
{
	public PlayStyle CurrentPlayStyle
	{
		get
		{
			foreach (Mod mod in server.api.ModLoader.Mods)
			{
				if (mod.WorldConfig == null)
				{
					continue;
				}
				PlayStyle[] playStyles = mod.WorldConfig.PlayStyles;
				foreach (PlayStyle ps in playStyles)
				{
					if (ps.Code == server.SaveGameData.PlayStyle)
					{
						return ps;
					}
				}
			}
			return null;
		}
	}

	public string CurrentWorldName => server.GetSaveFilename();

	public int MapSizeX => server.WorldMap.MapSizeX;

	public int MapSizeY => server.WorldMap.MapSizeY;

	public int MapSizeZ => server.WorldMap.MapSizeZ;

	public int ChunkSize => MagicNum.ServerChunkSize;

	public int RegionSize => MagicNum.ServerChunkSize * MagicNum.ChunkRegionSizeInChunks;

	public int Seed
	{
		get
		{
			if (server.SaveGameData == null)
			{
				throw new Exception("Game world not initialized yet, you need to call this method after the world has loaded, use the event GameWorldLoad.");
			}
			return server.SaveGameData.Seed;
		}
	}

	public bool AutoGenerateChunks
	{
		get
		{
			return server.AutoGenerateChunks;
		}
		set
		{
			server.AutoGenerateChunks = value;
		}
	}

	public bool SendChunks
	{
		get
		{
			return server.SendChunks;
		}
		set
		{
			server.SendChunks = value;
		}
	}

	public int[] DefaultSpawnPosition => new int[3]
	{
		server.SaveGameData.DefaultSpawn.x,
		server.SaveGameData.DefaultSpawn.y.Value,
		server.SaveGameData.DefaultSpawn.z
	};

	public ISaveGame SaveGame => server.SaveGameData;

	public Dictionary<long, IMapChunk> AllLoadedMapchunks
	{
		get
		{
			Dictionary<long, IMapChunk> dict = new Dictionary<long, IMapChunk>();
			foreach (KeyValuePair<long, ServerMapChunk> val in server.loadedMapChunks)
			{
				dict[val.Key] = val.Value;
			}
			return dict;
		}
	}

	public Dictionary<long, IServerChunk> AllLoadedChunks
	{
		get
		{
			Dictionary<long, IServerChunk> dict = new Dictionary<long, IServerChunk>();
			server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (KeyValuePair<long, ServerChunk> val in server.loadedChunks)
				{
					dict[val.Key] = val.Value;
				}
				return dict;
			}
			finally
			{
				server.loadedChunksLock.ReleaseReadLock();
			}
		}
	}

	public int CurrentGeneratingChunkCount => server.chunkThread.requestedChunkColumns.Count;

	public Dictionary<long, IMapRegion> AllLoadedMapRegions
	{
		get
		{
			Dictionary<long, IMapRegion> dict = new Dictionary<long, IMapRegion>();
			foreach (KeyValuePair<long, ServerMapRegion> val in server.loadedMapRegions)
			{
				dict[val.Key] = val.Value;
			}
			return dict;
		}
	}

	internal int RegionMapSizeX => server.WorldMap.RegionMapSizeX;

	public int ChunkDeletionsInQueue => server.deleteChunkColumns.Count;

	public WorldAPI(ServerMain server)
		: base(server)
	{
	}

	public IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		server.loadedMapRegions.TryGetValue(server.WorldMap.MapRegionIndex2D(regionX, regionZ), out var mapreg);
		return mapreg;
	}

	public IMapRegion GetMapRegion(long index2d)
	{
		server.loadedMapRegions.TryGetValue(index2d, out var mapreg);
		return mapreg;
	}

	public IServerMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		server.loadedMapChunks.TryGetValue(server.WorldMap.MapChunkIndex2D(chunkX, chunkZ), out var mapchunk);
		return mapchunk;
	}

	public IMapChunk GetMapChunk(long index2d)
	{
		server.loadedMapChunks.TryGetValue(index2d, out var mapchunk);
		return mapchunk;
	}

	public long MapChunkIndex2D(int chunkX, int chunkZ)
	{
		return server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
	}

	public IServerChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return server.WorldMap.GetServerChunk(chunkX, chunkY, chunkZ);
	}

	public IServerChunk GetChunk(BlockPos pos)
	{
		return (IServerChunk)server.WorldMap.GetChunk(pos);
	}

	public int? GetSurfacePosY(int posX, int posZ)
	{
		return server.WorldMap.GetTerrainGenSurfacePosY(posX, posZ);
	}

	public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
	{
		return new BlockAccessorCaching(server.WorldMap, server, synchronize, relight);
	}

	public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
	{
		if (strict)
		{
			return new BlockAccessorStrict(server.WorldMap, server, synchronize, relight, debug);
		}
		return new BlockAccessorRelaxed(server.WorldMap, server, synchronize, relight);
	}

	public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
	{
		return new BlockAccessorRelaxedBulkUpdate(server.WorldMap, server, synchronize, relight, debug);
	}

	public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
	{
		return new BlockAccessorRevertable(server.WorldMap, server, synchronize, relight, debug);
	}

	public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
	{
		return new BlockAccessorPrefetch(server.WorldMap, server, synchronize, relight);
	}

	public int GetBlockId(AssetLocation code)
	{
		if (!server.BlocksByCode.TryGetValue(code, out var block))
		{
			ServerMain.Logger.Error("GetBlockId(): Block with code '{0}' does not exist, defaulting to 0 for air", code);
			return 0;
		}
		return block.BlockId;
	}

	public void SetBlockLightLevels(float[] lightLevels)
	{
		server.SetBlockLightLevels(lightLevels);
	}

	public void SetSunLightLevels(float[] lightLevels)
	{
		server.SetSunLightLevels(lightLevels);
	}

	public void SetSunBrightness(int lightlevel)
	{
		server.SetSunBrightness(lightlevel);
	}

	public void SetSeaLevel(int sealevel)
	{
		server.SetSeaLevel(sealevel);
	}

	[Obsolete("Please use BlockPos version instead for dimension awareness")]
	public bool IsValidPos(int x, int y, int z)
	{
		return server.WorldMap.IsValidPos(x, y, z);
	}

	public byte[] GetData(string name)
	{
		return server.SaveGameData.GetData(name);
	}

	public void StoreData(string name, byte[] value)
	{
		server.SaveGameData.StoreData(name, value);
	}

	public void SetDefaultSpawnPosition(int x, int y, int z)
	{
		if (IsValidPos(x, y, z))
		{
			server.SaveGameData.DefaultSpawn.x = x;
			server.SaveGameData.DefaultSpawn.y = y;
			server.SaveGameData.DefaultSpawn.z = z;
			server.ConfigNeedsSaving = true;
		}
		else
		{
			ServerMain.Logger.Error("[Mod API] Invalid default spawn position suppplied!");
		}
	}

	public void SunFloodChunkColumnForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ)
	{
		int chunkY = chunks[0].MapChunk.YMax / ChunkSize;
		ushort sunLight = (ushort)server.sunBrightness;
		for (int cy = chunkY + 1; cy < server.WorldMap.ChunkMapSizeY; cy++)
		{
			IWorldChunk obj = chunks[cy];
			obj.Unpack();
			obj.Lighting.FloodWithSunlight(sunLight);
		}
		server.WorldMap.chunkIlluminatorWorldGen.Sunlight(chunks, chunkX, chunkY, chunkZ, 0);
		server.WorldMap.chunkIlluminatorWorldGen.SunlightFlood(chunks, chunkX, chunkY, chunkZ);
	}

	public void SunFloodChunkColumnNeighboursForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ)
	{
		IMapChunk mp1 = GetMapChunk(chunkX + 1, chunkZ);
		IMapChunk mp2 = GetMapChunk(chunkX - 1, chunkZ);
		IMapChunk mp3 = GetMapChunk(chunkX, chunkZ + 1);
		IMapChunk mp4 = GetMapChunk(chunkX, chunkZ - 1);
		int worldheight = server.WorldMap.MapSizeY;
		int chunkY = GameMath.Max(chunks[0].MapChunk.YMax, ((int?)mp1?.YMax) ?? (worldheight - 1), ((int?)mp2?.YMax) ?? (worldheight - 1), ((int?)mp3?.YMax) ?? (worldheight - 1), ((int?)mp4?.YMax) ?? (worldheight - 1)) / ChunkSize;
		server.WorldMap.chunkIlluminatorWorldGen.SunLightFloodNeighbourChunks(chunks, chunkX, chunkY, chunkZ, 0);
	}

	public void LoadChunkColumn(int chunkX, int chunkZ, bool keepLoaded = false)
	{
		server.LoadChunkColumn(chunkX, chunkZ, keepLoaded);
	}

	public void LoadChunkColumnFast(int chunkX, int chunkZ, ChunkLoadOptions options = null)
	{
		server.LoadChunkColumnFast(chunkX, chunkZ, options);
	}

	public void LoadChunkColumnPriority(int chunkX, int chunkZ, ChunkLoadOptions options = null)
	{
		server.LoadChunkColumnFast(chunkX, chunkZ, options);
	}

	public void LoadChunkColumnFast(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null)
	{
		server.LoadChunkColumnFast(chunkX1, chunkZ1, chunkX2, chunkZ2, options);
	}

	public void LoadChunkColumnPriority(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null)
	{
		server.LoadChunkColumnFast(chunkX1, chunkZ1, chunkX2, chunkZ2, options);
	}

	public void PeekChunkColumn(int chunkX, int chunkZ, ChunkPeekOptions options)
	{
		server.PeekChunkColumn(chunkX, chunkZ, options);
	}

	public void TestChunkExists(int chunkX, int chunkY, int chunkZ, Action<bool> onTested)
	{
		server.TestChunkExists(chunkX, chunkY, chunkZ, onTested, EnumChunkType.Chunk);
	}

	public void TestMapChunkExists(int chunkX, int chunkZ, Action<bool> onTested)
	{
		server.TestChunkExists(chunkX, 0, chunkZ, onTested, EnumChunkType.MapChunk);
	}

	public void TestMapRegionExists(int regionX, int regionZ, Action<bool> onTested)
	{
		server.TestChunkExists(regionX, 0, regionZ, onTested, EnumChunkType.MapRegion);
	}

	public void BroadcastChunk(int chunkX, int chunkY, int chunkZ, bool onlyIfInRange)
	{
		server.BroadcastChunk(chunkX, chunkY, chunkZ, onlyIfInRange);
	}

	public void SendChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player, bool onlyIfInRange)
	{
		server.SendChunk(chunkX, chunkY, chunkZ, player, onlyIfInRange);
	}

	public bool HasChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player)
	{
		return (player as ServerPlayer).client.DidSendChunk(server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ));
	}

	public void ResendMapChunk(int chunkX, int chunkZ, bool onlyIfInRange)
	{
		server.ResendMapChunk(chunkX, chunkZ, onlyIfInRange);
	}

	public void UnloadChunkColumn(int chunkX, int chunkZ)
	{
		long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		server.RemoveChunkColumnFromForceLoadedList(index2d);
		for (int cy2 = 0; cy2 < server.WorldMap.ChunkMapSizeY; cy2++)
		{
			long chunkIndex3d2 = server.WorldMap.ChunkIndex3D(chunkX, cy2, chunkZ);
			ServerChunk chunk = server.WorldMap.GetServerChunk(chunkIndex3d2);
			server.api.eventapi.TriggerChunkColumnUnloaded(new Vec3i(chunkX, cy2, chunkZ));
			if (chunk == null)
			{
				continue;
			}
			server.loadedChunksLock.AcquireWriteLock();
			try
			{
				if (server.loadedChunks.Remove(chunkIndex3d2))
				{
					server.ChunkColumnRequested.Remove(index2d);
				}
			}
			finally
			{
				server.loadedChunksLock.ReleaseWriteLock();
			}
			if (chunk.Entities != null)
			{
				for (int i = 0; i < chunk.Entities.Length; i++)
				{
					Entity e = chunk.Entities[i];
					if (e == null)
					{
						if (i >= chunk.EntitiesCount)
						{
							break;
						}
					}
					else if (!(e is EntityPlayer))
					{
						server.DespawnEntity(e, new EntityDespawnData
						{
							Reason = EnumDespawnReason.Unload
						});
					}
				}
			}
			foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in chunk.BlockEntities)
			{
				blockEntity.Value.OnBlockUnloaded();
			}
			chunk.Dispose();
		}
		for (int cy = 0; cy < server.WorldMap.ChunkMapSizeY; cy++)
		{
			long chunkIndex3d = server.WorldMap.ChunkIndex3D(chunkX, cy, chunkZ);
			server.unloadedChunks.Enqueue(chunkIndex3d);
		}
	}

	public void DeleteMapRegion(int regionX, int regionZ)
	{
		server.deleteMapRegions.Enqueue(server.WorldMap.MapRegionIndex2D(regionX, regionZ));
	}

	public void DeleteChunkColumn(int chunkX, int chunkZ)
	{
		UnloadChunkColumn(chunkX, chunkZ);
		server.deleteChunkColumns.Enqueue(server.WorldMap.MapChunkIndex2D(chunkX, chunkZ));
	}

	public void FullRelight(BlockPos minPos, BlockPos maxPos)
	{
		server.WorldMap.chunkIlluminatorMainThread.FullRelight(minPos, maxPos);
		int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X) - 32, 0, server.WorldMap.MapSizeX);
		int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y) - 32, 0, server.WorldMap.MapSizeY);
		int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z) - 32, 0, server.WorldMap.MapSizeZ);
		int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X) + 32, 0, server.WorldMap.MapSizeX);
		int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y) + 32, 0, server.WorldMap.MapSizeY);
		int num = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z) + 32, 0, server.WorldMap.MapSizeZ);
		int mincx = minx / 32;
		int mincy = miny / 32;
		int mincz = minz / 32;
		int maxcx = maxx / 32;
		int maxcy = maxy / 32;
		int maxcz = num / 32;
		for (int cx = mincx; cx <= maxcx; cx++)
		{
			for (int cy = mincy; cy <= maxcy; cy++)
			{
				for (int cz = mincz; cz <= maxcz; cz++)
				{
					server.BroadcastChunk(cx, cy, cz, onlyIfInRange: true);
					server.WorldMap.MarkChunkDirty(cx, cy, cz);
				}
			}
		}
	}

	public void FullRelight(BlockPos minPos, BlockPos maxPos, bool resendToClients)
	{
		if (resendToClients)
		{
			FullRelight(minPos, maxPos);
		}
		else
		{
			server.WorldMap.chunkIlluminatorMainThread.FullRelight(minPos, maxPos);
		}
	}

	public long GetNextUniqueId()
	{
		return server.GetNextHerdId();
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)server.WorldMap.index3dMulZ + chunkZ) * server.WorldMap.index3dMulX + chunkX;
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)RegionMapSizeX + regionX;
	}

	public Vec2i MapChunkPosFromChunkIndex2D(long chunkIndex2d)
	{
		return new Vec2i((int)(chunkIndex2d % server.WorldMap.ChunkMapSizeX), (int)(chunkIndex2d / server.WorldMap.ChunkMapSizeX));
	}

	public Vec3i MapRegionPosFromIndex2D(long index)
	{
		return new Vec3i((int)(index % RegionMapSizeX), 0, (int)(index / RegionMapSizeX));
	}

	public long MapRegionIndex2DByBlockPos(int posX, int posZ)
	{
		int regionX = posX / RegionSize;
		return (long)(posZ / RegionSize) * (long)RegionMapSizeX + regionX;
	}

	public IServerChunk GetChunk(long chunkIndex3d)
	{
		return server.GetLoadedChunk(chunkIndex3d);
	}

	public void CreateChunkColumnForDimension(int cx, int cz, int dim)
	{
		server.CreateChunkColumnForDimension(cx, cz, dim);
	}

	public void LoadChunkColumnForDimension(int cx, int cz, int dim)
	{
		server.LoadChunkColumnForDimension(cx, cz, dim);
	}

	public void ForceSendChunkColumn(IServerPlayer player, int cx, int cz, int dimension)
	{
		server.ForceSendChunkColumn(player, cx, cz, dimension);
	}

	public bool BlockingTestMapRegionExists(int regionX, int regionZ)
	{
		if (server.RunPhase >= EnumServerRunPhase.RunGame)
		{
			throw new InvalidOperationException("Can not be executed after EnumServerRunPhase.WorldReady");
		}
		return server.BlockingTestMapRegionExists(regionX, regionZ);
	}

	public bool BlockingTestMapChunkExists(int chunkX, int chunkZ)
	{
		if (server.RunPhase >= EnumServerRunPhase.RunGame)
		{
			throw new InvalidOperationException("Can not be executed after EnumServerRunPhase.WorldReady");
		}
		return server.BlockingTestMapChunkExists(chunkX, chunkZ);
	}

	public IServerChunk[] BlockingLoadChunkColumn(int chunkX, int chunkZ)
	{
		if (server.RunPhase >= EnumServerRunPhase.RunGame)
		{
			throw new InvalidOperationException("Can not be executed after EnumServerRunPhase.WorldReady");
		}
		return server.BlockingLoadChunkColumn(chunkX, chunkZ);
	}
}
