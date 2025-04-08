using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemSupplyChunks : ServerSystem
{
	private WorldGenHandler worldgenHandler;

	private ChunkServerThread chunkthread;

	private bool is8Core = Environment.ProcessorCount >= 8;

	private bool requiresChunkBorderSmoothing;

	private bool requiresSetEmptyFlag;

	private volatile int pauseAllWorldgenThreads;

	private List<long> entitiesToRemove = new List<long>();

	private int storyEventPrints;

	private string[] storyChunkSpawnEvents;

	private int blockingRequestsRemaining;

	public ServerSystemSupplyChunks(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		chunkthread.loadsavechunks = this;
		server.RegisterGameTickListener(delegate
		{
			server.serverChunkDataPool.SlowDispose();
		}, 1000);
	}

	public override int GetUpdateInterval()
	{
		if (chunkthread.requestedChunkColumns.Count == 0 || !is8Core)
		{
			return MagicNum.ChunkThreadTickTime;
		}
		return chunkthread.additionalWorldGenThreadsCount - 1;
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		requiresChunkBorderSmoothing = savegame.CreatedWorldGenVersion != 2;
		requiresSetEmptyFlag = GameVersion.IsLowerVersionThan(server.SaveGameData.LastSavedGameVersion, "1.12-dev.1");
		if (chunkthread.additionalWorldGenThreadsCount > 0)
		{
			int stageStart = 1;
			for (int i = 0; i < chunkthread.additionalWorldGenThreadsCount; i++)
			{
				CreateAdditionalWorldGenThread(stageStart, stageStart + 1, i + 1);
				stageStart++;
			}
		}
	}

	public override void OnSeparateThreadTick()
	{
		if (server.RunPhase != EnumServerRunPhase.RunGame)
		{
			return;
		}
		deleteChunkColumns();
		moveRequestsToGeneratingQueue();
		KeyValuePair<HorRectanglei, ChunkLoadOptions> val2;
		while (server.fastChunkQueue.Count > 0 && server.fastChunkQueue.TryDequeue(out val2))
		{
			loadChunkAreaBlocking(val2.Key.X1, val2.Key.Z1, val2.Key.X2, val2.Key.Z2, isStartupLoad: false, val2.Value.ChunkGenParams);
			if (val2.Value.OnLoaded != null)
			{
				server.EnqueueMainThreadTask(val2.Value.OnLoaded);
			}
		}
		if (server.simpleLoadRequests.Count > 0 && server.mapMiddleSpawnPos != null)
		{
			ChunkColumnLoadRequest loadRequest;
			while (server.simpleLoadRequests.TryDequeue(out loadRequest))
			{
				simplyLoadChunkColumn(loadRequest);
			}
		}
		if (server.peekChunkColumnQueue.Count > 0 && server.peekChunkColumnQueue.TryDequeue(out var val))
		{
			if (PauseAllWorldgenThreads(3600))
			{
				PeekChunkAreaLocking(val.Key, val.Value.UntilPass, val.Value.OnGenerated, val.Value.ChunkGenParams);
			}
			ResumeAllWorldgenThreads();
		}
		while (server.testChunkExistsQueue.Count > 0)
		{
			if (!server.testChunkExistsQueue.TryDequeue(out var val3))
			{
				break;
			}
			bool exists = false;
			switch (val3.Type)
			{
			case EnumChunkType.Chunk:
				exists = chunkthread.gameDatabase.ChunkExists(val3.chunkX, val3.chunkY, val3.chunkZ);
				break;
			case EnumChunkType.MapChunk:
				exists = chunkthread.gameDatabase.MapChunkExists(val3.chunkX, val3.chunkZ);
				break;
			case EnumChunkType.MapRegion:
				exists = chunkthread.gameDatabase.MapRegionExists(val3.chunkX, val3.chunkZ);
				break;
			}
			server.EnqueueMainThreadTask(delegate
			{
				val3.onTested(exists);
				ServerMain.FrameProfiler.Mark("MTT-TestExists");
			});
		}
		for (int i = 0; i < MagicNum.ChunkColumnsToGeneratePerThreadTick; i++)
		{
			if (!tryLoadOrGenerateChunkColumnsInQueue())
			{
				break;
			}
			if (server.Suspended)
			{
				break;
			}
		}
	}

	private void deleteChunkColumns()
	{
		List<ChunkPos> chunkCoords = new List<ChunkPos>();
		List<ChunkPos> mapChunkCoords = new List<ChunkPos>();
		long mapchunkindex2d;
		while (server.deleteChunkColumns.Count > 0 && server.deleteChunkColumns.TryDequeue(out mapchunkindex2d))
		{
			if (chunkthread.requestedChunkColumns.Remove(mapchunkindex2d))
			{
				server.ChunkColumnRequested.Remove(mapchunkindex2d);
			}
			ChunkPos pos = server.WorldMap.ChunkPosFromChunkIndex2D(mapchunkindex2d);
			UpdateLoadedNeighboursFlags(server.WorldMap, pos.X, pos.Z);
			for (int cy = 0; cy < server.WorldMap.ChunkMapSizeY; cy++)
			{
				ServerChunk chunk = (ServerChunk)server.WorldMap.GetChunk(pos.X, cy, pos.Z);
				if (chunk != null)
				{
					for (int i = 0; i < chunk.EntitiesCount; i++)
					{
						Entity entity = chunk.Entities[i];
						if (!(entity is EntityPlayer))
						{
							server.DespawnEntity(entity, new EntityDespawnData
							{
								Reason = EnumDespawnReason.Death
							});
						}
					}
				}
				chunkCoords.Add(new ChunkPos
				{
					X = pos.X,
					Y = cy,
					Z = pos.Z
				});
			}
			mapChunkCoords.Add(pos);
			server.loadedMapChunks.Remove(mapchunkindex2d);
		}
		if (chunkCoords.Count > 0)
		{
			chunkthread.gameDatabase.DeleteChunks(chunkCoords);
		}
		if (mapChunkCoords.Count > 0)
		{
			chunkthread.gameDatabase.DeleteMapChunks(mapChunkCoords);
		}
		HashSet<ChunkPos> mapRegionCoords = new HashSet<ChunkPos>();
		long mapregionIndex2d;
		while (server.deleteMapRegions.Count > 0 && server.deleteMapRegions.TryDequeue(out mapregionIndex2d))
		{
			ChunkPos regpos = server.WorldMap.MapRegionPosFromIndex2D(mapregionIndex2d);
			mapRegionCoords.Add(regpos);
			server.loadedMapRegions.Remove(mapregionIndex2d);
		}
		if (mapRegionCoords.Count > 0)
		{
			chunkthread.gameDatabase.DeleteMapRegions(mapRegionCoords);
		}
	}

	private void moveRequestsToGeneratingQueue()
	{
		List<long> elems = new List<long>();
		lock (server.requestedChunkColumnsLock)
		{
			while (server.requestedChunkColumns.Count > 0 && chunkthread.requestedChunkColumns.Capacity - chunkthread.requestedChunkColumns.Count >= elems.Count + 200)
			{
				elems.Add(server.requestedChunkColumns.Dequeue());
			}
		}
		for (int i = 0; i < elems.Count; i++)
		{
			long index2d = elems[i];
			Vec2i pos = server.WorldMap.MapChunkPosFromChunkIndex2D(index2d);
			chunkthread.addChunkColumnRequest(index2d, pos.X, pos.Y, -1);
		}
	}

	private bool simplyLoadChunkColumn(ChunkColumnLoadRequest request)
	{
		ServerMapChunk mapChunk = chunkthread.loadsavechunks.GetOrCreateMapChunk(request);
		if (mapChunk == null)
		{
			return false;
		}
		ServerChunk[] chunks = chunkthread.loadsavechunks.TryLoadChunkColumn(request);
		if (chunks == null)
		{
			return false;
		}
		foreach (ServerChunk obj in chunks)
		{
			obj.serverMapChunk = mapChunk;
			obj.MarkFresh();
		}
		request.MapChunk = mapChunk;
		request.Chunks = chunks;
		server.EnqueueMainThreadTask(delegate
		{
			chunkthread.loadsavechunks.mainThreadLoadChunkColumn(request);
		});
		return true;
	}

	private bool tryLoadOrGenerateChunkColumnsInQueue()
	{
		if (chunkthread.requestedChunkColumns.Count == 0)
		{
			return false;
		}
		PauseWorldgenThreadIfRequired(onChunkthread: true);
		CleanupRequestsQueue();
		int highestWorldgenPassOnOtherThreads = chunkthread.additionalWorldGenThreadsCount;
		foreach (ChunkColumnLoadRequest chunkRequest in chunkthread.requestedChunkColumns)
		{
			if (chunkRequest == null || chunkRequest.Disposed)
			{
				continue;
			}
			int curPass = chunkRequest.CurrentIncompletePass_AsInt;
			if (curPass == 0 || curPass > highestWorldgenPassOnOtherThreads)
			{
				if (server.Suspended)
				{
					return false;
				}
				if (loadOrGenerateChunkColumn_OnChunkThread(chunkRequest, curPass) && chunkthread.additionalWorldGenThreadsCount == 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	private int CleanupRequestsQueue()
	{
		int countRemoved = 0;
		ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest> requestedChunkColumns = chunkthread.requestedChunkColumns;
		while (requestedChunkColumns.Count > 0)
		{
			ChunkColumnLoadRequest chunkRequest = requestedChunkColumns.Peek();
			if (chunkRequest == null || chunkRequest.disposeOrRequeueFlags == 0)
			{
				break;
			}
			if (chunkRequest.Disposed)
			{
				requestedChunkColumns.DequeueWithoutRemovingFromIndex();
				countRemoved++;
			}
			else
			{
				chunkRequest.disposeOrRequeueFlags = 0;
				requestedChunkColumns.Requeue();
			}
		}
		return countRemoved;
	}

	public bool loadOrGenerateChunkColumn_OnChunkThread(ChunkColumnLoadRequest chunkRequest, int stage)
	{
		ServerMapChunk mapChunk = GetOrCreateMapChunk(chunkRequest);
		if (chunkRequest.Chunks == null)
		{
			if (mapChunk.WorldGenVersion != 2 && mapChunk.CurrentIncompletePass < EnumWorldGenPass.Done)
			{
				mapChunk = GetOrCreateMapChunk(chunkRequest, forceCreate: true);
				mapChunk.MarkDirty();
			}
			else
			{
				chunkRequest.Chunks = TryLoadChunkColumn(chunkRequest);
				if (chunkRequest.Chunks != null)
				{
					for (int y = 0; y < chunkRequest.Chunks.Length; y++)
					{
						ServerChunk obj = chunkRequest.Chunks[y];
						obj.serverMapChunk = mapChunk;
						obj.MarkFresh();
					}
				}
			}
			int regionX = chunkRequest.chunkX / (server.api.WorldManager.RegionSize / server.api.WorldManager.ChunkSize);
			int regionZ = chunkRequest.chunkZ / (server.api.WorldManager.RegionSize / server.api.WorldManager.ChunkSize);
			long regionIndex = server.api.WorldManager.MapRegionIndex2D(regionX, regionZ);
			IMapRegion mapRegion = server.api.WorldManager.GetMapRegion(regionIndex);
			if (mapRegion != null)
			{
				TryRestoreGeneratedStructures(regionX, regionZ, chunkRequest.chunkGenParams, mapRegion);
			}
			if (chunkRequest.Chunks == null)
			{
				GenerateNewChunkColumn(mapChunk, chunkRequest);
			}
		}
		chunkRequest.MapChunk = mapChunk;
		if (chunkRequest.CurrentIncompletePass == EnumWorldGenPass.Done)
		{
			if (chunkRequest.blockingRequest && --blockingRequestsRemaining == 0)
			{
				ServerMain.Logger.VerboseDebug("Completed area loading/generation");
			}
			runScheduledBlockUpdatesWithNeighbours(chunkRequest);
			try
			{
				ServerEventAPI eventapi = server.api.eventapi;
				ServerMapChunk mapChunk2 = mapChunk;
				int chunkX = chunkRequest.chunkX;
				int chunkZ = chunkRequest.chunkZ;
				IWorldChunk[] chunks = chunkRequest.Chunks;
				eventapi.TriggerBeginChunkColumnLoadChunkThread(mapChunk2, chunkX, chunkZ, chunks);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Exception throwing during chunk Unpack() at chunkpos xz {0}/{1}. Likely corrupted. Exception: {2}", chunkRequest.chunkX, chunkRequest.chunkZ, e);
				if (server.Config.RepairMode)
				{
					ServerMain.Logger.Error("Repair mode is enabled so will delete the entire chunk column.");
					GenerateNewChunkColumn(mapChunk, chunkRequest);
					return false;
				}
			}
			server.EnqueueMainThreadTask(delegate
			{
				mainThreadLoadChunkColumn(chunkRequest);
				chunkthread.requestedChunkColumns.elementsByIndex.Remove(chunkRequest.Index);
			});
			chunkRequest.FlagToDispose();
			return true;
		}
		if (mapChunk.currentpass != stage && mapChunk.currentpass <= chunkthread.additionalWorldGenThreadsCount)
		{
			return stage == 0;
		}
		bool num = CanGenerateChunkColumn(chunkRequest);
		if (num)
		{
			PopulateChunk(chunkRequest);
		}
		if (!num || chunkthread.additionalWorldGenThreadsCount == 0)
		{
			chunkRequest.FlagToRequeue();
		}
		return num;
	}

	public int GenerateChunkColumns_OnSeparateThread(int stageStart, int stageEnd)
	{
		ChunkColumnLoadRequest newestRequiringWork = null;
		long newestTime = long.MinValue;
		foreach (ChunkColumnLoadRequest chunkRequest in chunkthread.requestedChunkColumns)
		{
			if (chunkRequest == null || chunkRequest.Disposed)
			{
				continue;
			}
			int stage = chunkRequest.CurrentIncompletePass_AsInt;
			if (stage < stageStart || stage >= stageEnd)
			{
				continue;
			}
			if (stage >= chunkRequest.untilPass)
			{
				chunkRequest.FlagToRequeue();
			}
			else if (chunkRequest.creationTime > newestTime)
			{
				if (ensurePrettyNeighbourhood(chunkRequest))
				{
					newestTime = chunkRequest.creationTime;
					newestRequiringWork = chunkRequest;
				}
				else
				{
					chunkRequest.FlagToRequeue();
				}
			}
		}
		if (newestRequiringWork != null)
		{
			PopulateChunk(newestRequiringWork);
			return 1;
		}
		return 0;
	}

	public bool CanGenerateChunkColumn(ChunkColumnLoadRequest chunkRequest)
	{
		if (chunkRequest.CurrentIncompletePass_AsInt >= chunkRequest.untilPass || !ensurePrettyNeighbourhood(chunkRequest))
		{
			return false;
		}
		return true;
	}

	internal void mainThreadLoadChunkColumn(ChunkColumnLoadRequest chunkRequest)
	{
		if (server.RunPhase == EnumServerRunPhase.Shutdown)
		{
			return;
		}
		ServerMain.FrameProfiler.Enter("MTT-ChunkLoaded-Begin");
		ServerEventAPI eventapi = server.api.eventapi;
		Vec2i chunkCoord = new Vec2i(chunkRequest.chunkX, chunkRequest.chunkZ);
		IWorldChunk[] chunks = chunkRequest.Chunks;
		eventapi.TriggerChunkColumnLoaded(chunkCoord, chunks);
		ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-LoadedEvent");
		for (int yIndex = 0; yIndex < chunkRequest.Chunks.Length; yIndex++)
		{
			ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-LoadedEvent");
			ServerChunk chunk = chunkRequest.Chunks[yIndex];
			int chunkY2 = yIndex + chunkRequest.dimension * 1024;
			long index3d = server.WorldMap.ChunkIndex3D(chunkRequest.chunkX, chunkY2, chunkRequest.chunkZ);
			server.loadedChunksLock.AcquireWriteLock();
			try
			{
				if (server.loadedChunks.ContainsKey(index3d))
				{
					continue;
				}
				server.loadedChunks[index3d] = chunk;
				chunk.MarkToPack();
				goto IL_00ff;
			}
			finally
			{
				server.loadedChunksLock.ReleaseWriteLock();
			}
			IL_00ff:
			if (server.Config.AnalyzeMode)
			{
				try
				{
					chunk.Unpack();
				}
				catch (Exception e3)
				{
					ServerMain.Logger.Error("Exception throwing during chunk Unpack() at chunkpos {0}/{1}/{2}, dimension {4}. Likely corrupted. Exception: {3}", chunkRequest.chunkX, yIndex, chunkRequest.chunkZ, e3, chunkRequest.dimension);
					if (server.Config.RepairMode && chunkRequest.dimension == 0)
					{
						ServerMain.Logger.Error("Repair mode is enabled so will delete the entire chunk column.");
						server.api.worldapi.DeleteChunkColumn(chunkRequest.chunkX, chunkRequest.chunkZ);
						ServerMain.FrameProfiler.Leave();
						return;
					}
				}
			}
			entitiesToRemove.Clear();
			if (chunk.Entities != null)
			{
				for (int i = 0; i < chunk.Entities.Length; i++)
				{
					Entity e2 = chunk.Entities[i];
					if (e2 == null)
					{
						if (i >= chunk.EntitiesCount)
						{
							break;
						}
					}
					else if (!server.LoadEntity(e2, index3d))
					{
						entitiesToRemove.Add(e2.EntityId);
					}
				}
			}
			foreach (long entityId in entitiesToRemove)
			{
				chunk.RemoveEntity(entityId);
			}
			ServerMain.FrameProfiler.Enter("MTT-ChunkLoaded-LoadBlockEntities");
			foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in chunk.BlockEntities)
			{
				BlockEntity be = blockEntity.Value;
				if (be == null)
				{
					continue;
				}
				try
				{
					ServerMain.FrameProfiler.Enter(be.Block.Code.Path);
					be.Initialize(server.api);
					if (chunk.serverMapChunk.NewBlockEntities.Contains(be.Pos))
					{
						chunk.serverMapChunk.NewBlockEntities.Remove(be.Pos);
						be.OnBlockPlaced();
					}
					ServerMain.FrameProfiler.Leave();
				}
				catch (Exception e)
				{
					ServerMain.Logger.Notification("Exception thrown when trying to initialize a block entity @{0}: {1}", be.Pos, e);
					be.UnregisterAllTickListeners();
				}
			}
			ServerMain.FrameProfiler.Leave();
			server.api.eventapi.TriggerChunkDirty(new Vec3i(chunkRequest.chunkX, chunkY2, chunkRequest.chunkZ), chunk, EnumChunkDirtyReason.NewlyLoaded);
		}
		ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-MarkDirtyEvent");
		updateNeighboursLoadedFlags(chunkRequest.MapChunk, chunkRequest.chunkX, chunkRequest.chunkZ);
		ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-UpdateNeighboursFlags");
		for (int chunkY = 0; chunkY < chunkRequest.Chunks.Length; chunkY++)
		{
			chunkRequest.Chunks[chunkY].TryPackAndCommit();
		}
		ServerMain.FrameProfiler.Mark("MTT-ChunkLoaded-Pack");
		ServerMain.FrameProfiler.Leave();
	}

	private void updateNeighboursLoadedFlags(ServerMapChunk mapChunk, int chunkX, int chunkZ)
	{
		mapChunk.NeighboursLoaded = default(SmallBoolArray);
		mapChunk.SelfLoaded = true;
		for (int i = 0; i < Cardinal.ALL.Length; i++)
		{
			Cardinal cd = Cardinal.ALL[i];
			ServerMapChunk mc = (ServerMapChunk)server.WorldMap.GetMapChunk(chunkX + cd.Normali.X, chunkZ + cd.Normali.Z);
			if (mc != null)
			{
				mapChunk.NeighboursLoaded[i] = mc.SelfLoaded;
				mc.NeighboursLoaded[cd.Opposite.Index] = true;
			}
		}
	}

	public static void UpdateLoadedNeighboursFlags(ServerWorldMap WorldMap, int chunkX, int chunkZ)
	{
		ServerMapChunk mcNorth = (ServerMapChunk)WorldMap.GetMapChunk(chunkX, chunkZ - 1);
		ServerMapChunk mcNorthEast = (ServerMapChunk)WorldMap.GetMapChunk(chunkX + 1, chunkZ - 1);
		ServerMapChunk mcEast = (ServerMapChunk)WorldMap.GetMapChunk(chunkX + 1, chunkZ);
		ServerMapChunk mcSouthEast = (ServerMapChunk)WorldMap.GetMapChunk(chunkX + 1, chunkZ + 1);
		ServerMapChunk mcSouth = (ServerMapChunk)WorldMap.GetMapChunk(chunkX, chunkZ + 1);
		ServerMapChunk mcSouthWest = (ServerMapChunk)WorldMap.GetMapChunk(chunkX - 1, chunkZ + 1);
		ServerMapChunk mcWest = (ServerMapChunk)WorldMap.GetMapChunk(chunkX - 1, chunkZ);
		ServerMapChunk mcNorthWest = (ServerMapChunk)WorldMap.GetMapChunk(chunkX - 1, chunkZ - 1);
		if (mcNorth != null)
		{
			mcNorth.NeighboursLoaded[4] = false;
		}
		if (mcNorthEast != null)
		{
			mcNorthEast.NeighboursLoaded[5] = false;
		}
		if (mcEast != null)
		{
			mcEast.NeighboursLoaded[6] = false;
		}
		if (mcSouthEast != null)
		{
			mcSouthEast.NeighboursLoaded[7] = false;
		}
		if (mcSouth != null)
		{
			mcSouth.NeighboursLoaded[0] = false;
		}
		if (mcSouthWest != null)
		{
			mcSouthWest.NeighboursLoaded[1] = false;
		}
		if (mcWest != null)
		{
			mcWest.NeighboursLoaded[2] = false;
		}
		if (mcNorthWest != null)
		{
			mcNorthWest.NeighboursLoaded[3] = false;
		}
	}

	private void runScheduledBlockUpdatesWithNeighbours(ChunkColumnLoadRequest chunkRequest)
	{
		for (int z = -1; z <= 1; z++)
		{
			for (int x = -1; x <= 1; x++)
			{
				bool doScheduledBlockUpdates = false;
				if (server.loadedMapChunks.TryGetValue(server.WorldMap.MapChunkIndex2D(chunkRequest.chunkX + x, chunkRequest.chunkZ + z), out var mpc) && mpc.CurrentIncompletePass == EnumWorldGenPass.Done && mpc.ScheduledBlockUpdates.Count > 0 && areAllChunkNeighboursLoaded(mpc, chunkRequest.chunkX + x, chunkRequest.chunkZ + z))
				{
					doScheduledBlockUpdates = true;
				}
				if (!doScheduledBlockUpdates)
				{
					continue;
				}
				foreach (BlockPos pos in mpc.ScheduledBlockUpdates)
				{
					server.WorldMap.MarkBlockModified(pos);
				}
				mpc.ScheduledBlockUpdates.Clear();
			}
		}
	}

	private bool areAllChunkNeighboursLoaded(ServerMapChunk mpc, int chunkX, int chunkZ)
	{
		int neibsloaded = 0;
		for (int dz = -1; dz <= 1; dz++)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				if ((dx != 0 || dz != 0) && server.loadedMapChunks.TryGetValue(server.WorldMap.MapChunkIndex2D(chunkX + dx, chunkZ + dz), out var neibmpc) && neibmpc.CurrentIncompletePass == EnumWorldGenPass.Done)
				{
					neibsloaded++;
				}
			}
		}
		return neibsloaded == 8;
	}

	private ServerMapRegion GetOrCreateMapRegionEnsureNeighbours(int chunkX, int chunkZ, ITreeAttribute chunkGenParams)
	{
		ServerMapRegion mapRegion = GetOrCreateMapRegion(chunkX, chunkZ, chunkGenParams);
		if (!mapRegion.NeighbourRegionsChecked)
		{
			int regionX = chunkX / MagicNum.ChunkRegionSizeInChunks;
			int regionZ = chunkZ / MagicNum.ChunkRegionSizeInChunks;
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dz = -1; dz <= 1; dz++)
				{
					if (regionX + dx >= 0 && regionZ + dz >= 0)
					{
						GetOrCreateMapRegion((regionX + dx) * MagicNum.ChunkRegionSizeInChunks, (regionZ + dz) * MagicNum.ChunkRegionSizeInChunks, chunkGenParams);
					}
				}
			}
			mapRegion.NeighbourRegionsChecked = true;
		}
		return mapRegion;
	}

	private ServerMapRegion GetOrCreateMapRegion(int chunkX, int chunkZ, ITreeAttribute chunkGenParams)
	{
		int regionX = chunkX / MagicNum.ChunkRegionSizeInChunks;
		int regionZ = chunkZ / MagicNum.ChunkRegionSizeInChunks;
		ServerMapRegion mapRegion = null;
		long mapRegionIndex2d = server.WorldMap.MapRegionIndex2D(regionX, regionZ);
		server.loadedMapRegions.TryGetValue(mapRegionIndex2d, out mapRegion);
		if (mapRegion != null)
		{
			return mapRegion;
		}
		mapRegion = TryLoadMapRegion(regionX, regionZ);
		if (mapRegion != null)
		{
			mapRegion.loadedTotalMs = server.ElapsedMilliseconds;
			server.loadedMapRegions[mapRegionIndex2d] = mapRegion;
			server.EnqueueMainThreadTask(delegate
			{
				server.api.eventapi.TriggerMapRegionLoaded(new Vec2i(regionX, regionZ), mapRegion);
				ServerMain.FrameProfiler.Mark("trigger-mapregionloaded");
			});
			return mapRegion;
		}
		mapRegion = CreateMapRegion(regionX, regionZ, chunkGenParams);
		mapRegion.loadedTotalMs = server.ElapsedMilliseconds;
		server.loadedMapRegions[mapRegionIndex2d] = mapRegion;
		server.EnqueueMainThreadTask(delegate
		{
			server.api.eventapi.TriggerMapRegionLoaded(new Vec2i(regionX, regionZ), mapRegion);
			ServerMain.FrameProfiler.Mark("trigger-mapregionloaded");
		});
		return mapRegion;
	}

	private ServerMapRegion CreateMapRegion(int regionX, int regionZ, ITreeAttribute chunkGenParams)
	{
		ServerMapRegion mapRegion = ServerMapRegion.CreateNew();
		for (int i = 0; i < worldgenHandler.OnMapRegionGen.Count; i++)
		{
			worldgenHandler.OnMapRegionGen[i](mapRegion, regionX, regionZ, chunkGenParams);
		}
		return mapRegion;
	}

	private void TryRestoreGeneratedStructures(int regionX, int regionZ, ITreeAttribute chunkGenParams, IMapRegion mapRegion)
	{
		byte[] structureData = chunkGenParams?.GetBytes("GeneratedStructures");
		if (structureData == null)
		{
			return;
		}
		Dictionary<long, List<GeneratedStructure>> dictionary = SerializerUtil.Deserialize<Dictionary<long, List<GeneratedStructure>>>(structureData);
		long regionIndex = server.api.WorldManager.MapRegionIndex2D(regionX, regionZ);
		if (!dictionary.TryGetValue(regionIndex, out var generatedStructures) || generatedStructures == null)
		{
			return;
		}
		mapRegion.GeneratedStructures.AddRange(generatedStructures.Where((GeneratedStructure structure) => !mapRegion.GeneratedStructures.Any((GeneratedStructure s) => s.Location.Start.Equals(structure.Location.Start))));
	}

	internal ServerMapChunk GetOrCreateMapChunk(ChunkColumnLoadRequest chunkRequest, bool forceCreate = false)
	{
		int chunkX = chunkRequest.chunkX;
		int chunkZ = chunkRequest.chunkZ;
		ServerMapRegion mapRegion = GetOrCreateMapRegionEnsureNeighbours(chunkX, chunkZ, chunkRequest.chunkGenParams);
		long mapChunkIndex2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ServerMapChunk mapChunk;
		if (!forceCreate)
		{
			server.loadedMapChunks.TryGetValue(mapChunkIndex2d, out mapChunk);
			if (mapChunk != null)
			{
				return mapChunk;
			}
			mapChunk = TryLoadMapChunk(chunkX, chunkZ, mapRegion);
			if (mapChunk != null)
			{
				mapChunk.MapRegion = mapRegion;
				server.loadedMapChunks[mapChunkIndex2d] = mapChunk;
				return mapChunk;
			}
		}
		mapChunk = CreateMapChunk(chunkX, chunkZ, mapRegion);
		server.loadedMapChunks[mapChunkIndex2d] = mapChunk;
		return mapChunk;
	}

	private ServerMapChunk PeekMapChunk(int chunkX, int chunkZ)
	{
		long mapChunkIndex2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		server.loadedMapChunks.TryGetValue(mapChunkIndex2d, out var mapChunk);
		if (mapChunk != null)
		{
			return mapChunk;
		}
		return TryLoadMapChunk(chunkX, chunkZ, null);
	}

	private ServerMapChunk CreateMapChunk(int chunkX, int chunkZ, ServerMapRegion mapRegion)
	{
		ServerMapChunk mapChunk = ServerMapChunk.CreateNew(mapRegion);
		for (int i = 0; i < worldgenHandler.OnMapChunkGen.Count; i++)
		{
			worldgenHandler.OnMapChunkGen[i](mapChunk, chunkX, chunkZ);
		}
		return mapChunk;
	}

	private void GenerateNewChunkColumn(ServerMapChunk mapChunk, ChunkColumnLoadRequest chunkRequest)
	{
		int quantity = server.WorldMap.ChunkMapSizeY;
		chunkRequest.Chunks = new ServerChunk[quantity];
		for (int y = 0; y < quantity; y++)
		{
			ServerChunk chunk = ServerChunk.CreateNew(server.serverChunkDataPool);
			chunk.serverMapChunk = mapChunk;
			chunkRequest.Chunks[y] = chunk;
		}
		chunkRequest.MapChunk = mapChunk;
		if (requiresChunkBorderSmoothing)
		{
			for (int i = 0; i < Cardinal.ALL.Length; i++)
			{
				Cardinal cd = Cardinal.ALL[i];
				ServerMapChunk mc = PeekMapChunk(chunkRequest.chunkX + cd.Normali.X, chunkRequest.chunkZ + cd.Normali.Z);
				if (mc != null && mc.CurrentIncompletePass >= EnumWorldGenPass.NeighbourSunLightFlood && mc.WorldGenVersion != 2)
				{
					if (chunkRequest.NeighbourTerrainHeight == null)
					{
						chunkRequest.NeighbourTerrainHeight = new ushort[8][];
					}
					chunkRequest.NeighbourTerrainHeight[i] = mc.WorldGenTerrainHeightMap;
				}
			}
			chunkRequest.RequiresChunkBorderSmoothing = chunkRequest.NeighbourTerrainHeight != null;
			if (mapChunk.CurrentIncompletePass < EnumWorldGenPass.NeighbourSunLightFlood && mapChunk.WorldGenVersion != 2)
			{
				mapChunk.WorldGenVersion = 2;
				GenerateNewChunkColumn(mapChunk, chunkRequest);
			}
		}
		chunkRequest.CurrentIncompletePass = EnumWorldGenPass.Terrain;
	}

	internal ServerChunk[] TryLoadChunkColumn(ChunkColumnLoadRequest chunkRequest)
	{
		int quantity = server.WorldMap.ChunkMapSizeY;
		ServerChunk[] chunks = new ServerChunk[quantity];
		int loaded = 0;
		for (int y = 0; y < quantity; y++)
		{
			byte[] serializedChunk = chunkthread.gameDatabase.GetChunk(chunkRequest.chunkX, y, chunkRequest.chunkZ, chunkRequest.dimension);
			if (serializedChunk == null)
			{
				continue;
			}
			try
			{
				loaded++;
				chunks[y] = ServerChunk.FromBytes(serializedChunk, server.serverChunkDataPool, server);
			}
			catch (Exception e)
			{
				if (server.Config.RegenerateCorruptChunks || server.Config.RepairMode)
				{
					chunks[y] = ServerChunk.CreateNew(server.serverChunkDataPool);
					ServerMain.Logger.Error("Failed deserializing a chunk, we are in repair mode, so will initilize empty one. Exception: {0}", e);
					continue;
				}
				ServerMain.Logger.Error("Failed deserializing a chunk. Not in repair mode, will exit.");
				throw;
			}
		}
		if (requiresSetEmptyFlag)
		{
			foreach (ServerChunk chunk in chunks)
			{
				if (chunk != null)
				{
					chunk.Unpack();
					chunk.MarkModified();
					chunk.TryPackAndCommit();
				}
			}
		}
		if (loaded != 0 && loaded != quantity)
		{
			ServerMain.Logger.Error("Loaded some but not all chunks of a column? Discarding whole column.");
			return null;
		}
		if (loaded != quantity)
		{
			return null;
		}
		return chunks;
	}

	private ServerMapChunk TryLoadMapChunk(int chunkX, int chunkZ, ServerMapRegion forRegion)
	{
		byte[] serializedMapChunk = chunkthread.gameDatabase.GetMapChunk(chunkX, chunkZ);
		if (serializedMapChunk != null)
		{
			try
			{
				ServerMapChunk mapchunk = ServerMapChunk.FromBytes(serializedMapChunk);
				if (GameVersion.IsLowerVersionThan(server.SaveGameData.CreatedGameVersion, "1.7"))
				{
					mapchunk.YMax = (ushort)(server.SaveGameData.MapSizeY - 1);
				}
				return mapchunk;
			}
			catch (Exception e)
			{
				if (!server.Config.RegenerateCorruptChunks && !server.Config.RepairMode)
				{
					ServerMain.Logger.Error("Failed deserializing a map chunk. Not in repair mode, will exit.");
					throw;
				}
				ServerMain.Logger.Error("Failed deserializing a map chunk, we are in repair mode, so will initialize empty one. Exception: {0}", e);
				ServerMapChunk serverMapChunk = ServerMapChunk.CreateNew(forRegion);
				serverMapChunk.MarkDirty();
				return serverMapChunk;
			}
		}
		return null;
	}

	private ServerMapRegion TryLoadMapRegion(int regionX, int regionZ)
	{
		byte[] serializedMapRegion = chunkthread.gameDatabase.GetMapRegion(regionX, regionZ);
		try
		{
			if (serializedMapRegion != null)
			{
				return ServerMapRegion.FromBytes(serializedMapRegion);
			}
		}
		catch (Exception e)
		{
			if (server.Config.RepairMode)
			{
				ServerMain.Logger.Error("Failed deserializing a map region, we are in repair mode, so will initialize empty one.");
				ServerMain.Logger.Error(e);
				return null;
			}
			ServerMain.Logger.Error("Failed deserializing a map region. Not in repair mode, will exit.");
			throw;
		}
		return null;
	}

	public void InitWorldgenAndSpawnChunks()
	{
		worldgenHandler = (WorldGenHandler)server.api.Event.TriggerInitWorldGen();
		ServerMain.Logger.Event("Loading {0}x{1}x{2} spawn chunks...", MagicNum.SpawnChunksWidth, MagicNum.SpawnChunksWidth, server.WorldMap.ChunkMapSizeY);
		if (storyChunkSpawnEvents == null)
		{
			storyChunkSpawnEvents = new string[15]
			{
				Lang.Get("...the carved mountains"),
				Lang.Get("...the rolling hills"),
				Lang.Get("...the vertical cliffs"),
				Lang.Get("...the endless plains"),
				Lang.Get("...the winter lands"),
				Lang.Get("...and scorching deserts"),
				Lang.Get("...spring waters"),
				Lang.Get("...tunnels deep below"),
				Lang.Get("...the luscious trees"),
				Lang.Get("...the fragrant flowers"),
				Lang.Get("...the roaming creatures"),
				Lang.Get("with their offspring..."),
				Lang.Get("...a misty sunrise"),
				Lang.Get("...dew drops on a blade of grass"),
				Lang.Get("...a soft breeze")
			};
		}
		BlockPos pos = new BlockPos(server.WorldMap.MapSizeX / 2, 0, server.WorldMap.MapSizeZ / 2, 0);
		if (GameVersion.IsLowerVersionThan(server.SaveGameData.CreatedGameVersion, "1.20.0-pre.14"))
		{
			int dcx = 0;
			int dcz = 0;
			bool found = false;
			Random rand = new Random(server.Seed);
			int maxTries = 5;
			int maxRadiusToTry = 20;
			for (int tries = 0; tries < maxTries; tries++)
			{
				if (tries > 0)
				{
					double maxRadiusThisAttempt = (double)GameMath.Sqrt((double)tries * (1.0 / (double)maxTries)) * (double)maxRadiusToTry;
					double num = (1.0 - Math.Abs(rand.NextDouble() - rand.NextDouble())) * maxRadiusThisAttempt;
					double rndAngle = rand.NextDouble() * 6.2831854820251465;
					double offsetX = num * GameMath.Sin(rndAngle);
					double num2 = num * GameMath.Cos(rndAngle);
					dcx = (int)offsetX;
					dcz = (int)num2;
					pos = new BlockPos(dcx * 32 + server.WorldMap.MapSizeX / 2, 0, dcz * 32 + server.WorldMap.MapSizeZ / 2, 0);
				}
				loadChunkAreaBlocking(dcx + server.WorldMap.ChunkMapSizeX / 2 - MagicNum.SpawnChunksWidth / 2, dcz + server.WorldMap.ChunkMapSizeZ / 2 - MagicNum.SpawnChunksWidth / 2, dcx + server.WorldMap.ChunkMapSizeX / 2 + MagicNum.SpawnChunksWidth / 2, dcz + server.WorldMap.ChunkMapSizeZ / 2 + MagicNum.SpawnChunksWidth / 2, isStartupLoad: true);
				server.ProcessMainThreadTasks();
				if (AdjustForSaveSpawnSpot(server, pos, null, rand))
				{
					found = true;
					break;
				}
				if (tries + 1 < maxTries)
				{
					server.api.Logger.Notification("Trying another spawn location ({0}/{1})...", tries + 2, maxTries);
				}
			}
			if (!found)
			{
				pos = new BlockPos(server.WorldMap.MapSizeX / 2, 0, server.WorldMap.MapSizeZ / 2, 0);
				pos.Y = server.blockAccessor.GetRainMapHeightAt(pos);
				if (!server.blockAccessor.GetBlock(pos).SideSolid[BlockFacing.UP.Index])
				{
					server.blockAccessor.SetBlock(server.blockAccessor.GetBlock(new AssetLocation("planks-oak-we")).Id, pos);
				}
				pos.Y++;
			}
		}
		else
		{
			loadChunkAreaBlocking(server.WorldMap.ChunkMapSizeX / 2 - MagicNum.SpawnChunksWidth / 2, server.WorldMap.ChunkMapSizeZ / 2 - MagicNum.SpawnChunksWidth / 2, server.WorldMap.ChunkMapSizeX / 2 + MagicNum.SpawnChunksWidth / 2, server.WorldMap.ChunkMapSizeZ / 2 + MagicNum.SpawnChunksWidth / 2, isStartupLoad: true);
			server.ProcessMainThreadTasks();
		}
		server.mapMiddleSpawnPos = new PlayerSpawnPos
		{
			x = pos.X,
			y = pos.Y,
			z = pos.Z
		};
		if (pos.Y < 0)
		{
			server.mapMiddleSpawnPos.y = null;
		}
		server.api.Logger.VerboseDebug("Done spawn chunk");
	}

	public static bool AdjustForSaveSpawnSpot(ServerMain server, BlockPos pos, IServerPlayer forPlayer, Random rand)
	{
		int tries = 60;
		int dx = 0;
		int dz = 0;
		int posx = 0;
		int posy = 0;
		int posz = 0;
		while (tries-- > 0)
		{
			posx = GameMath.Clamp(dx + pos.X, 0, server.WorldMap.MapSizeX - 1);
			posz = GameMath.Clamp(dz + pos.Z, 0, server.WorldMap.MapSizeZ - 1);
			posy = server.WorldMap.GetTerrainGenSurfacePosY(posx, posz);
			pos.Set(posx, posy, posz);
			dx = rand.Next(64) - 32;
			dz = rand.Next(64) - 32;
			if (posy != 0 && !((double)posy > 0.75 * (double)server.WorldMap.MapSizeY))
			{
				if (server.WorldMap.GetBlockingLandClaimant(forPlayer, pos, EnumBlockAccessFlags.Use) != null)
				{
					server.api.Logger.Notification("Spawn pos blocked at " + pos);
				}
				else if (!server.BlockAccessor.GetBlock(posx, posy + 1, posz, 2).IsLiquid() && !server.BlockAccessor.GetBlock(posx, posy, posz, 2).IsLiquid() && !server.BlockAccessor.IsSideSolid(posx, posy + 1, posz, BlockFacing.UP))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void PeekChunkAreaLocking(Vec2i coords, EnumWorldGenPass untilPass, OnChunkPeekedDelegate onGenerated, ITreeAttribute chunkGenParams)
	{
		chunkthread.peekMode = true;
		int centerCx = coords.X;
		int centerCz = coords.Y;
		Dictionary<Vec2i, ServerMapRegion> regions = new Dictionary<Vec2i, ServerMapRegion>();
		int nowPass = 1;
		int endPass = Math.Min((int)untilPass, 5);
		int startRadius = endPass - nowPass;
		ChunkColumnLoadRequest[,] reqs = new ChunkColumnLoadRequest[startRadius * 2 + 1, startRadius * 2 + 1];
		for (int cx3 = -startRadius; cx3 <= startRadius; cx3++)
		{
			for (int cz3 = -startRadius; cz3 <= startRadius; cz3++)
			{
				long index2d2 = server.WorldMap.MapChunkIndex2D(centerCx + cx3, centerCz + cz3);
				int regionX = (centerCx + cx3) / MagicNum.ChunkRegionSizeInChunks;
				int regionZ = (centerCz + cz3) / MagicNum.ChunkRegionSizeInChunks;
				Vec2i regionCoord = new Vec2i(regionX, regionZ);
				ServerMapRegion mapregion = null;
				if (!regions.TryGetValue(regionCoord, out mapregion))
				{
					mapregion = (regions[regionCoord] = CreateMapRegion(regionX, regionZ, chunkGenParams));
				}
				ServerMapChunk mapchunk = CreateMapChunk(centerCx, centerCz, mapregion);
				ChunkColumnLoadRequest chunkRequest3 = new ChunkColumnLoadRequest(index2d2, centerCx + cx3, centerCz + cz3, 0, (int)untilPass, server)
				{
					chunkGenParams = ((cx3 == 0 && cz3 == 0) ? chunkGenParams : null)
				};
				chunkRequest3.MapChunk = mapchunk;
				GenerateNewChunkColumn(mapchunk, chunkRequest3);
				chunkRequest3.Unpack();
				reqs[cx3 + startRadius, cz3 + startRadius] = chunkRequest3;
				lock (chunkthread.peekingChunkColumns)
				{
					chunkthread.peekingChunkColumns.Enqueue(chunkRequest3);
				}
			}
		}
		int radius = endPass - nowPass;
		for (; nowPass <= endPass; nowPass++)
		{
			radius = endPass - nowPass;
			for (int cx2 = -radius; cx2 <= radius; cx2++)
			{
				for (int cz2 = -radius; cz2 <= radius; cz2++)
				{
					ChunkColumnLoadRequest chunkRequest2 = reqs[cx2 + startRadius, cz2 + startRadius];
					runGenerators(chunkRequest2, nowPass);
				}
			}
		}
		Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate = new Dictionary<Vec2i, IServerChunk[]>();
		lock (chunkthread.peekingChunkColumns)
		{
			for (int cx = -startRadius; cx <= startRadius; cx++)
			{
				for (int cz = -startRadius; cz <= startRadius; cz++)
				{
					long index2d = server.WorldMap.MapChunkIndex2D(centerCx + cx, centerCz + cz);
					chunkthread.peekingChunkColumns.Remove(index2d);
					ChunkColumnLoadRequest chunkRequest = reqs[cx + startRadius, cz + startRadius];
					Dictionary<Vec2i, IServerChunk[]> dictionary = columnsByChunkCoordinate;
					Vec2i key = new Vec2i(centerCx + cx, centerCz + cz);
					IServerChunk[] chunks = chunkRequest.Chunks;
					dictionary[key] = chunks;
				}
			}
		}
		chunkthread.peekMode = false;
		server.EnqueueMainThreadTask(delegate
		{
			onGenerated(columnsByChunkCoordinate);
			ServerMain.FrameProfiler.Mark("MTT-PeekChunk");
		});
	}

	private void loadChunkAreaBlocking(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, bool isStartupLoad = false, ITreeAttribute chunkGenParams = null)
	{
		ResumeAllWorldgenThreads();
		int startQuantity = server.loadedChunks.Count / server.WorldMap.ChunkMapSizeY;
		int toGenerate = (chunkX2 - chunkX1 + 1) * (chunkZ2 - chunkZ1 + 1);
		CleanupRequestsQueue();
		moveRequestsToGeneratingQueue();
		blockingRequestsRemaining = 0;
		foreach (ChunkColumnLoadRequest requestedChunkColumn in chunkthread.requestedChunkColumns)
		{
			requestedChunkColumn.blockingRequest = false;
		}
		for (int chunkX3 = chunkX1; chunkX3 <= chunkX2; chunkX3++)
		{
			for (int chunkZ3 = chunkZ1; chunkZ3 <= chunkZ2; chunkZ3++)
			{
				if (server.WorldMap.IsValidChunkPos(chunkX3, 0, chunkZ3) && !server.IsChunkColumnFullyLoaded(chunkX3, chunkZ3))
				{
					ChunkColumnLoadRequest req = new ChunkColumnLoadRequest(server.WorldMap.MapChunkIndex2D(chunkX3, chunkZ3), chunkX3, chunkZ3, server.serverConsoleId, 6, server)
					{
						chunkGenParams = chunkGenParams
					};
					req.blockingRequest = true;
					if (chunkthread.addChunkColumnRequest(req))
					{
						blockingRequestsRemaining++;
					}
				}
			}
		}
		long timeout = Environment.TickCount + 12000;
		ServerMain.Logger.VerboseDebug("Starting area loading/generation: columns " + blockingRequestsRemaining + ", total queue length " + chunkthread.requestedChunkColumns.Count);
		while (!server.stopped && !server.exit.exit)
		{
			CleanupRequestsQueue();
			if (blockingRequestsRemaining <= 0 || chunkthread.requestedChunkColumns.Count == 0)
			{
				break;
			}
			if (isStartupLoad && server.totalUnpausedTime.ElapsedMilliseconds - millisecondsSinceStart > 1500)
			{
				millisecondsSinceStart = server.totalUnpausedTime.ElapsedMilliseconds;
				float completion = 100f * ((float)server.loadedChunks.Count / (float)server.WorldMap.ChunkMapSizeY - (float)startQuantity) / (float)toGenerate;
				ServerMain.Logger.Event(completion.ToString("0.#") + "% ({0} in queue)", chunkthread.requestedChunkColumns.Count);
				if (storyEventPrints < storyChunkSpawnEvents.Length)
				{
					ServerMain.Logger.StoryEvent(storyChunkSpawnEvents[storyEventPrints]);
				}
				else
				{
					ServerMain.Logger.StoryEvent("...");
				}
				storyEventPrints++;
			}
			bool doneAny = false;
			foreach (ChunkColumnLoadRequest request2 in chunkthread.requestedChunkColumns)
			{
				if (request2 == null || request2.Disposed)
				{
					continue;
				}
				int curPass = request2.CurrentIncompletePass_AsInt;
				if (curPass == 0 || curPass > chunkthread.additionalWorldGenThreadsCount)
				{
					if (server.exit.exit || server.stopped)
					{
						return;
					}
					doneAny |= loadOrGenerateChunkColumn_OnChunkThread(request2, curPass);
				}
			}
			if (doneAny)
			{
				timeout = Environment.TickCount + 12000;
			}
			else
			{
				if (Environment.TickCount <= timeout)
				{
					continue;
				}
				ServerMain.Logger.Error("Attempting to force generate chunk columns from " + chunkX1 + "," + chunkZ1 + " to " + chunkX2 + "," + chunkZ2);
				ServerMain.Logger.Error(chunkthread.additionalWorldGenThreadsCount + " additional worldgen threads active, number of 'undone' chunks is " + blockingRequestsRemaining);
				foreach (ChunkColumnLoadRequest request in chunkthread.requestedChunkColumns)
				{
					if (request != null)
					{
						string inset = ((request.chunkX >= chunkX1 && request.chunkX <= chunkX2 && request.chunkZ >= chunkZ1 && request.chunkZ <= chunkZ2) ? " (in original req)" : "");
						ServerMain.Logger.Error("Column " + request.ChunkX + "," + request.ChunkZ + " has reached pass " + request.CurrentIncompletePass_AsInt + inset);
					}
				}
				throw new Exception("Somehow worldgen has become stuck in an endless loop, please report this as a bug!  Additional data in the server-main log");
			}
		}
	}

	private void PopulateChunk(ChunkColumnLoadRequest chunkRequest)
	{
		chunkRequest.Unpack();
		chunkRequest.generatingLock.AcquireWriteLock();
		try
		{
			if (server.Config.SkipEveryChunkRow > 0 && chunkRequest.chunkX % (server.Config.SkipEveryChunkRow + server.Config.SkipEveryChunkRowWidth) < server.Config.SkipEveryChunkRowWidth)
			{
				if (chunkRequest.CurrentIncompletePass == EnumWorldGenPass.Terrain)
				{
					ushort defaultSunlight = (ushort)server.sunBrightness;
					for (int y = 0; y < chunkRequest.Chunks.Length; y++)
					{
						chunkRequest.Chunks[y].Lighting.ClearWithSunlight(defaultSunlight);
					}
				}
			}
			else
			{
				runGenerators(chunkRequest, chunkRequest.MapChunk.currentpass);
			}
			chunkRequest.MapChunk.WorldGenVersion = 2;
			for (int i = 0; i < chunkRequest.Chunks.Length; i++)
			{
				chunkRequest.Chunks[i].MarkModified();
			}
			chunkRequest.MapChunk.currentpass++;
			chunkRequest.MapChunk.DirtyForSaving = true;
		}
		finally
		{
			chunkRequest.generatingLock.ReleaseWriteLock();
		}
	}

	private void runGenerators(ChunkColumnLoadRequest chunkRequest, int forPass)
	{
		List<ChunkColumnGenerationDelegate> handlers = worldgenHandler.OnChunkColumnGen[forPass];
		if (handlers == null)
		{
			return;
		}
		for (int i = 0; i < handlers.Count; i++)
		{
			try
			{
				handlers[i](chunkRequest);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Worldgen("An error was thrown in pass {5} when generating chunk column X={0},Z={1} in world '{3}' with seed {4}\nException {2}\n\n", chunkRequest.chunkX, chunkRequest.chunkZ, e, server.SaveGameData.WorldName, server.SaveGameData.Seed, chunkRequest.CurrentIncompletePass.ToString());
				if (chunkRequest.CurrentIncompletePass <= EnumWorldGenPass.Terrain)
				{
					break;
				}
			}
		}
	}

	private bool ensurePrettyNeighbourhood(ChunkColumnLoadRequest chunkRequest)
	{
		if (chunkRequest.CurrentIncompletePass <= EnumWorldGenPass.Terrain)
		{
			return true;
		}
		bool pretty = true;
		int minPass = chunkRequest.CurrentIncompletePass_AsInt;
		int minx = Math.Max(chunkRequest.chunkX - 1, 0);
		int maxx = Math.Min(chunkRequest.chunkX + 1, server.WorldMap.ChunkMapSizeX - 1);
		int minz = Math.Max(chunkRequest.chunkZ - 1, 0);
		int maxz = Math.Min(chunkRequest.chunkZ + 1, server.WorldMap.ChunkMapSizeZ - 1);
		if (!chunkRequest.prettified && !EnsureQueueSpace(chunkRequest))
		{
			return false;
		}
		for (int chunkX = minx; chunkX <= maxx; chunkX++)
		{
			for (int chunkZ = minz; chunkZ <= maxz; chunkZ++)
			{
				if (chunkX == chunkRequest.chunkX && chunkZ == chunkRequest.chunkZ)
				{
					continue;
				}
				long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
				if (!chunkthread.EnsureMinimumWorldgenPassAt(index2d, chunkX, chunkZ, minPass, chunkRequest.creationTime))
				{
					if (chunkRequest.prettified)
					{
						return false;
					}
					pretty = false;
				}
			}
		}
		chunkRequest.prettified = true;
		return pretty;
	}

	private EnumWorldGenPass getLoadedOrQueuedChunkPass(long index2d)
	{
		server.loadedMapChunks.TryGetValue(index2d, out var mapchunk);
		if (mapchunk == null || mapchunk.CurrentIncompletePass != EnumWorldGenPass.Done)
		{
			return chunkthread.requestedChunkColumns.GetByIndex(index2d)?.CurrentIncompletePass ?? EnumWorldGenPass.None;
		}
		return EnumWorldGenPass.Done;
	}

	private bool EnsureQueueSpace(ChunkColumnLoadRequest curRequest)
	{
		int requestToDrop = chunkthread.requestedChunkColumns.Count + 30 - chunkthread.requestedChunkColumns.Capacity;
		if (requestToDrop <= 0)
		{
			return true;
		}
		if (!PauseAllWorldgenThreads(5000))
		{
			return false;
		}
		try
		{
			ServerMain.Logger.Warning("Requested chunks buffer is too small! Taking measures to attempt to free enough space. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
			((ServerSystemUnloadChunks)chunkthread.serversystems[2]).UnloadGeneratingChunkColumns(MagicNum.UncompressedChunkTTL / 10);
			foreach (ChunkColumnLoadRequest item in chunkthread.requestedChunkColumns.Snapshot())
			{
				item.FlagToRequeue();
			}
			requestToDrop -= CleanupRequestsQueue();
			if (requestToDrop <= 0)
			{
				return true;
			}
			ServerMain.Logger.Error("Requested chunks buffer is too small! Can't free enough space to completely generate chunks, clearing whole buffer. This may cause issues. Try increasing servermagicnumbers RequestChunkColumnsQueueSize and/or reducing UncompressedChunkTTL.");
			FullyClearGeneratingQueue();
			chunkthread.requestedChunkColumns.Enqueue(curRequest);
			return true;
		}
		finally
		{
			ResumeAllWorldgenThreads();
			if (chunkthread.additionalWorldGenThreadsCount > 0)
			{
				ServerMain.Logger.VerboseDebug("Un-pausing all worldgen threads.");
			}
		}
	}

	internal void FullyClearGeneratingQueue()
	{
		chunkthread.loadsavegame.SaveAllDirtyGeneratingChunks();
		foreach (ChunkColumnLoadRequest req in chunkthread.requestedChunkColumns)
		{
			if (req != null && !req.Disposed)
			{
				server.loadedMapChunks.Remove(req.mapIndex2d);
				server.ChunkColumnRequested.Remove(req.mapIndex2d);
			}
		}
		chunkthread.requestedChunkColumns.Clear();
		ServerMain.Logger.VerboseDebug("Incomplete chunks stored and wiped.");
	}

	private Thread CreateAdditionalWorldGenThread(int stageStart, int stageEnd, int threadnum)
	{
		Thread thread = TyronThreadPool.CreateDedicatedThread(delegate
		{
			GeneratorThreadLoop(stageStart, stageEnd);
		}, "worldgen" + threadnum);
		thread.Start();
		return thread;
	}

	public void GeneratorThreadLoop(int stageStart, int stageEnd)
	{
		Thread.Sleep(5);
		int tries = Math.Min(3, MagicNum.ChunkColumnsToGeneratePerThreadTick / chunkthread.additionalWorldGenThreadsCount);
		int columnsDone = 0;
		while (!server.stopped)
		{
			if (chunkthread.requestedChunkColumns.Count > 0 && (!server.Suspended || server.RunPhase == EnumServerRunPhase.WorldReady))
			{
				try
				{
					for (int i = 0; i < tries; i++)
					{
						int done = GenerateChunkColumns_OnSeparateThread(stageStart, stageEnd);
						columnsDone += done;
						if (done == 0)
						{
							break;
						}
					}
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error(e);
				}
			}
			PauseWorldgenThreadIfRequired(onChunkthread: false);
			Thread.Sleep(1);
		}
		BlockAccessorWorldGen.ThreadDispose();
	}

	public override void OnSeperateThreadShutDown()
	{
		BlockAccessorWorldGen.ThreadDispose();
	}

	public override void Dispose()
	{
		BlockAccessorWorldGen.ThreadDispose();
	}

	public bool PauseAllWorldgenThreads(int timeoutms)
	{
		if (Interlocked.CompareExchange(ref pauseAllWorldgenThreads, 1, 0) != 0)
		{
			return false;
		}
		if (chunkthread.additionalWorldGenThreadsCount > 0)
		{
			ServerMain.Logger.VerboseDebug("Pausing all worldgen threads.");
		}
		long maxTime = Environment.TickCount + timeoutms;
		while (pauseAllWorldgenThreads < chunkthread.additionalWorldGenThreadsCount + 1 && !server.stopped)
		{
			if (Environment.TickCount > maxTime)
			{
				ServerMain.Logger.VerboseDebug("Pausing all worldgen threads - exceeded timeout!");
				return false;
			}
			Thread.Sleep(1);
		}
		return true;
	}

	public void ResumeAllWorldgenThreads()
	{
		pauseAllWorldgenThreads = 0;
	}

	public void PauseWorldgenThreadIfRequired(bool onChunkthread)
	{
		if (pauseAllWorldgenThreads <= 0)
		{
			return;
		}
		Interlocked.Increment(ref pauseAllWorldgenThreads);
		while (pauseAllWorldgenThreads != 0 && !server.stopped)
		{
			if (onChunkthread)
			{
				chunkthread.paused = true;
			}
			Thread.Sleep(15);
		}
		if (onChunkthread)
		{
			chunkthread.paused = server.Suspended || chunkthread.ShouldPause;
		}
	}
}
