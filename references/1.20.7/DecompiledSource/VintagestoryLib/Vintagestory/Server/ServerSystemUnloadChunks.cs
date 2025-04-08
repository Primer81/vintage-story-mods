using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemUnloadChunks : ServerSystem
{
	private ChunkServerThread chunkthread;

	private bool unloadingPaused;

	private HashSet<long> mapChunkUnloadCandidates = new HashSet<long>();

	private object mapChunkIndicesLock = new object();

	private List<long> mapChunkIndices = new List<long>(800);

	private object dirtyChunksLock = new object();

	private List<ServerChunkWithCoord> dirtyUnloadedChunks = new List<ServerChunkWithCoord>();

	private List<ServerMapChunkWithCoord> dirtyUnloadedMapChunks = new List<ServerMapChunkWithCoord>();

	private object dirtyMapRegionsLock = new object();

	private List<MapRegionAndPos> dirtyMapRegions = new List<MapRegionAndPos>();

	private float accum120s;

	private float accum3s;

	public ServerSystemUnloadChunks(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		server.api.ChatCommands.GetOrCreate("chunk").BeginSubCommand("unload").WithDescription("Toggle on / off whether the server(and thus in turn the client) should unload chunks")
			.WithAdditionalInformation("Default setting is on. This should normally be left on.")
			.WithArgs(server.api.ChatCommands.Parsers.Bool("setting"))
			.HandleWith(handleToggleUnload)
			.EndSubCommand();
	}

	public override void OnBeginModsAndConfigReady()
	{
		server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(OnPlayerLeaveChunk);
	}

	public override void OnBeginShutdown()
	{
		foreach (KeyValuePair<long, ServerChunk> val2 in server.loadedChunks)
		{
			ChunkPos pos2 = server.WorldMap.ChunkPosFromChunkIndex3D(val2.Key);
			if (pos2.Dimension <= 0)
			{
				server.api.eventapi.TriggerChunkColumnUnloaded(pos2.ToVec3i());
			}
		}
		foreach (KeyValuePair<long, ServerMapRegion> val in server.loadedMapRegions)
		{
			ChunkPos pos = server.WorldMap.MapRegionPosFromIndex2D(val.Key);
			server.api.eventapi.TriggerMapRegionUnloaded(new Vec2i(pos.X, pos.Z), val.Value);
		}
	}

	private void OnPlayerLeaveChunk(ClientStatistics stats)
	{
		if (!unloadingPaused)
		{
			SendOutOfRangeChunkUnloads(stats.client);
		}
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		if (server.Clients.Count == 1)
		{
			ServerMain.Logger.Notification("Last player disconnected, compacting large object heap...");
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
		}
	}

	private TextCommandResult handleToggleUnload(TextCommandCallingArgs args)
	{
		unloadingPaused = !(bool)args[0];
		return TextCommandResult.Success("Chunk unloading now " + (unloadingPaused ? "off" : "on"));
	}

	public override int GetUpdateInterval()
	{
		return 200;
	}

	public override void OnServerTick(float dt)
	{
		if (unloadingPaused)
		{
			return;
		}
		ServerMain.FrameProfiler.Enter("unloadchunks-all");
		int count = server.unloadedChunks.Count;
		SendUnloadedChunkUnloads();
		ServerMain.FrameProfiler.Mark("notified-clients (" + count + ")");
		accum3s += dt;
		if (accum3s >= 3f)
		{
			accum3s = 0f;
			FindUnloadableChunkColumnCandidates();
			ServerMain.FrameProfiler.Mark("find-chunkcolumns");
			if (mapChunkUnloadCandidates.Count > 0)
			{
				UnloadChunkColumns();
				if (server.Clients.Count == 0)
				{
					server.serverChunkDataPool.FreeAll();
					GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					GC.Collect();
					ServerMain.FrameProfiler.Mark("garbagecollector (no clients online)");
				}
			}
		}
		accum120s += dt;
		if (accum120s > 120f)
		{
			accum120s = 0f;
			FindUnusedMapRegions();
			ServerMain.FrameProfiler.Mark("find-mapregions");
		}
		ServerMain.FrameProfiler.Leave();
	}

	private void FindUnusedMapRegions()
	{
		List<long> regionsToClear = new List<long>();
		List<MapRegionAndPos> regionsToSave = null;
		foreach (KeyValuePair<long, ServerMapRegion> val2 in server.loadedMapRegions)
		{
			if (server.ElapsedMilliseconds - val2.Value.loadedTotalMs < 120000)
			{
				continue;
			}
			ChunkPos pos = server.WorldMap.MapRegionPosFromIndex2D(val2.Key);
			int blockx = pos.X * server.WorldMap.RegionSize;
			int num = pos.Z * server.WorldMap.RegionSize;
			int chunkx = blockx / 32;
			int chunkz = num / 32;
			if (server.WorldMap.AnyLoadedChunkInMapRegion(chunkx, chunkz))
			{
				continue;
			}
			regionsToClear.Add(val2.Key);
			server.api.eventapi.TriggerMapRegionUnloaded(new Vec2i(pos.X, pos.Z), val2.Value);
			if (val2.Value.DirtyForSaving)
			{
				if (regionsToSave == null)
				{
					regionsToSave = new List<MapRegionAndPos>();
				}
				regionsToSave.Add(new MapRegionAndPos(pos.ToVec3i(), val2.Value));
			}
		}
		if (regionsToSave != null)
		{
			lock (dirtyMapRegionsLock)
			{
				foreach (MapRegionAndPos toSave in regionsToSave)
				{
					dirtyMapRegions.Add(toSave);
					toSave.region.DirtyForSaving = false;
				}
			}
		}
		foreach (long val in regionsToClear)
		{
			server.loadedMapRegions.Remove(val);
			server.BroadcastUnloadMapRegion(val);
		}
	}

	public override void OnSeparateThreadTick()
	{
		if (server.RunPhase != EnumServerRunPhase.Shutdown && !unloadingPaused)
		{
			lock (mapChunkIndicesLock)
			{
				mapChunkIndices.Clear();
				mapChunkIndices.AddRange(server.loadedMapChunks.Keys);
			}
			SaveDirtyUnloadedChunks();
			SaveDirtyMapRegions();
			UnloadGeneratingChunkColumns(MagicNum.UncompressedChunkTTL);
		}
	}

	private void SaveDirtyMapRegions()
	{
		if (dirtyMapRegions.Count <= 0)
		{
			return;
		}
		List<MapRegionAndPos> toSave = new List<MapRegionAndPos>();
		lock (dirtyMapRegionsLock)
		{
			toSave.AddRange(dirtyMapRegions);
			dirtyMapRegions.Clear();
		}
		List<DbChunk> cp = new List<DbChunk>();
		foreach (MapRegionAndPos val in toSave)
		{
			cp.Add(new DbChunk(new ChunkPos(val.pos), val.region.ToBytes()));
		}
		chunkthread.gameDatabase.SetMapRegions(cp);
	}

	private void SaveDirtyUnloadedChunks()
	{
		server.readyToAutoSave = false;
		List<ServerChunkWithCoord> dirtyChunksTmp = new List<ServerChunkWithCoord>();
		List<ServerMapChunkWithCoord> dirtyMapChunksTmp = new List<ServerMapChunkWithCoord>();
		lock (dirtyChunksLock)
		{
			dirtyChunksTmp.AddRange(dirtyUnloadedChunks);
			dirtyMapChunksTmp.AddRange(dirtyUnloadedMapChunks);
			dirtyUnloadedChunks.Clear();
			dirtyUnloadedMapChunks.Clear();
		}
		List<DbChunk> dirtyDbChunks = new List<DbChunk>();
		List<DbChunk> dirtyDbMapChunks = new List<DbChunk>();
		using FastMemoryStream reusableStream = new FastMemoryStream();
		foreach (ServerChunkWithCoord data2 in dirtyChunksTmp)
		{
			dirtyDbChunks.Add(new DbChunk
			{
				Position = data2.pos,
				Data = data2.chunk.ToBytes(reusableStream)
			});
			data2.chunk.Dispose();
		}
		foreach (ServerMapChunkWithCoord data in dirtyMapChunksTmp)
		{
			dirtyDbMapChunks.Add(new DbChunk
			{
				Position = new ChunkPos
				{
					X = data.chunkX,
					Y = 0,
					Z = data.chunkZ
				},
				Data = data.mapchunk.ToBytes(reusableStream)
			});
		}
		if (dirtyDbChunks.Count > 0)
		{
			chunkthread.gameDatabase.SetChunks(dirtyDbChunks);
		}
		if (dirtyDbMapChunks.Count > 0)
		{
			chunkthread.gameDatabase.SetMapChunks(dirtyDbMapChunks);
		}
		server.readyToAutoSave = true;
	}

	private void UnloadChunkColumns()
	{
		List<ServerChunkWithCoord> dirtyChunksTmp = new List<ServerChunkWithCoord>();
		List<ServerMapChunkWithCoord> dirtyMapChunksTmp = new List<ServerMapChunkWithCoord>();
		int cUnloaded = 0;
		foreach (long index2d in mapChunkUnloadCandidates)
		{
			if (server.forceLoadedChunkColumns.Contains(index2d))
			{
				continue;
			}
			ChunkPos ret = server.WorldMap.ChunkPosFromChunkIndex2D(index2d);
			ServerSystemSupplyChunks.UpdateLoadedNeighboursFlags(server.WorldMap, ret.X, ret.Z);
			server.api.eventapi.TriggerChunkColumnUnloaded(ret.ToVec3i());
			for (int y = 0; y < server.WorldMap.ChunkMapSizeY; y++)
			{
				ret.Y = y;
				long posIndex3d = server.WorldMap.ChunkIndex3D(ret.X, y, ret.Z);
				ServerChunk chunk = server.GetLoadedChunk(posIndex3d);
				if (chunk != null && TryUnloadChunk(posIndex3d, ret, chunk, dirtyChunksTmp, server))
				{
					cUnloaded++;
				}
			}
			ServerMapChunk mapchunk = null;
			server.loadedMapChunks.TryGetValue(index2d, out mapchunk);
			if (mapchunk != null)
			{
				if (mapchunk.DirtyForSaving)
				{
					dirtyMapChunksTmp.Add(new ServerMapChunkWithCoord
					{
						chunkX = ret.X,
						chunkZ = ret.Z,
						index2d = index2d,
						mapchunk = mapchunk
					});
				}
				mapchunk.DirtyForSaving = false;
				server.loadedMapChunks.Remove(index2d);
			}
		}
		lock (dirtyChunksLock)
		{
			dirtyUnloadedChunks.AddRange(dirtyChunksTmp);
			dirtyUnloadedMapChunks.AddRange(dirtyMapChunksTmp);
		}
		ServerMain.FrameProfiler.Mark("unloaded-chunkcolumns (" + mapChunkUnloadCandidates.Count + ")");
		mapChunkUnloadCandidates.Clear();
	}

	public static bool TryUnloadChunk(long posIndex3d, ChunkPos ret, ServerChunk chunk, List<ServerChunkWithCoord> dirtyChunksTmp, ServerMain server)
	{
		bool mustSave = false;
		if (chunk.DirtyForSaving)
		{
			mustSave = true;
			dirtyChunksTmp.Add(new ServerChunkWithCoord
			{
				pos = ret,
				chunk = chunk
			});
		}
		chunk.DirtyForSaving = false;
		server.unloadedChunks.Enqueue(posIndex3d);
		long index2d = server.WorldMap.ChunkIndex3dToIndex2d(posIndex3d);
		server.loadedChunksLock.AcquireWriteLock();
		try
		{
			if (server.loadedChunks.Remove(posIndex3d))
			{
				server.ChunkColumnRequested.Remove(index2d);
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseWriteLock();
		}
		chunk.RemoveEntitiesAndBlockEntities(server);
		if (!mustSave)
		{
			chunk.Dispose();
		}
		return mustSave;
	}

	internal void UnloadGeneratingChunkColumns(long timeToLive)
	{
		List<ChunkColumnLoadRequest> toUnload = new List<ChunkColumnLoadRequest>();
		int cUnloaded = 0;
		foreach (ChunkColumnLoadRequest chunkreq in chunkthread.requestedChunkColumns.Snapshot())
		{
			if (chunkreq.Chunks == null || chunkreq.Disposed)
			{
				continue;
			}
			EnumWorldGenPass curPass = chunkreq.CurrentIncompletePass;
			if (curPass < chunkreq.GenerateUntilPass || curPass == EnumWorldGenPass.Done)
			{
				continue;
			}
			bool unload = true;
			if (server.forceLoadedChunkColumns.Contains(chunkreq.mapIndex2d))
			{
				continue;
			}
			for (int y2 = 0; y2 < chunkreq.Chunks.Length; y2++)
			{
				if (Environment.TickCount - chunkreq.Chunks[y2].lastReadOrWrite < timeToLive)
				{
					unload = false;
					break;
				}
			}
			if (unload)
			{
				toUnload.Add(chunkreq);
			}
		}
		if (toUnload.Count == 0)
		{
			return;
		}
		List<DbChunk> dirtyChunks = new List<DbChunk>();
		List<DbChunk> dirtyMapChunks = new List<DbChunk>();
		using FastMemoryStream reusableStream = new FastMemoryStream();
		foreach (ChunkColumnLoadRequest req in toUnload)
		{
			req.generatingLock.AcquireReadLock();
			try
			{
				for (int y = 0; y < req.Chunks.Length; y++)
				{
					if (req.Chunks[y].DirtyForSaving)
					{
						req.Chunks[y].DirtyForSaving = false;
						dirtyChunks.Add(new DbChunk
						{
							Position = new ChunkPos(req.chunkX, y, req.chunkZ, 0),
							Data = req.Chunks[y].ToBytes(reusableStream)
						});
					}
				}
				ServerMapChunk mapchunk = req.MapChunk;
				if (mapchunk != null)
				{
					if (mapchunk.DirtyForSaving)
					{
						dirtyMapChunks.Add(new DbChunk
						{
							Position = new ChunkPos(req.chunkX, 0, req.chunkZ, 0),
							Data = mapchunk.ToBytes(reusableStream)
						});
					}
					mapchunk.DirtyForSaving = false;
					server.loadedMapChunks.Remove(req.mapIndex2d);
				}
			}
			finally
			{
				req.generatingLock.ReleaseReadLock();
			}
			if (!chunkthread.requestedChunkColumns.Remove(req.mapIndex2d))
			{
				throw new Exception("Chunkrequest no longer in queue? Race condition?");
			}
			server.ChunkColumnRequested.Remove(req.mapIndex2d);
			cUnloaded++;
		}
		if (dirtyChunks.Count > 0)
		{
			chunkthread.gameDatabase.SetChunks(dirtyChunks);
		}
		if (dirtyMapChunks.Count > 0)
		{
			chunkthread.gameDatabase.SetMapChunks(dirtyMapChunks);
		}
	}

	private void FindUnloadableChunkColumnCandidates()
	{
		List<long> index2ds = new List<long>();
		foreach (ConnectedClient client in server.Clients.Values)
		{
			int allowedChunkRadius = server.GetAllowedChunkRadius(client);
			int chunkX = ((client.Position == null) ? (server.WorldMap.MapSizeX / 2) : ((int)client.Position.X)) / MagicNum.ServerChunkSize;
			int chunkZ = ((client.Position == null) ? (server.WorldMap.MapSizeZ / 2) : ((int)client.Position.Z)) / MagicNum.ServerChunkSize;
			for (int r = 0; r <= allowedChunkRadius; r++)
			{
				ShapeUtil.LoadOctagonIndices(index2ds, chunkX, chunkZ, r, server.WorldMap.ChunkMapSizeX);
			}
		}
		Vec2i vec = new Vec2i();
		ServerMapChunk mapchunk;
		foreach (long item in index2ds)
		{
			MapUtil.PosInt2d(item, server.WorldMap.ChunkMapSizeX, vec);
			long mapchunkindex2d = server.WorldMap.MapChunkIndex2D(vec.X, vec.Y);
			server.loadedMapChunks.TryGetValue(mapchunkindex2d, out mapchunk);
			mapchunk?.MarkFresh();
		}
		lock (mapChunkIndicesLock)
		{
			foreach (long index2d in mapChunkIndices)
			{
				if (!server.forceLoadedChunkColumns.Contains(index2d) && server.loadedMapChunks.TryGetValue(index2d, out mapchunk) && mapchunk.CurrentIncompletePass == EnumWorldGenPass.Done)
				{
					if (mapchunk.IsOld())
					{
						mapChunkUnloadCandidates.Add(index2d);
					}
					else
					{
						mapchunk.DoAge();
					}
				}
			}
		}
	}

	private void SendUnloadedChunkUnloads()
	{
		if (server.unloadedChunks.Count == 0)
		{
			return;
		}
		List<long> unloadIndices = new List<long>();
		unloadIndices.AddRange(server.unloadedChunks);
		server.unloadedChunks = new ConcurrentQueue<long>();
		List<Vec3i> ulCoordForPlayer = new List<Vec3i>();
		foreach (ConnectedClient client in server.Clients.Values)
		{
			ulCoordForPlayer.Clear();
			foreach (long index3d in unloadIndices)
			{
				if (client.ChunkSent.Contains(index3d))
				{
					int cx = (int)(index3d % server.WorldMap.index3dMulX);
					int cy = (int)(index3d / server.WorldMap.index3dMulX / server.WorldMap.index3dMulZ);
					int cz = (int)(index3d / server.WorldMap.index3dMulX % server.WorldMap.index3dMulZ);
					ulCoordForPlayer.Add(new Vec3i(cx, cy, cz));
					client.RemoveChunkSent(index3d);
					long index2d = server.WorldMap.ChunkIndex3dToIndex2d(index3d);
					client.RemoveMapChunkSent(index2d);
				}
			}
			if (ulCoordForPlayer.Count > 0)
			{
				int[] xr = new int[ulCoordForPlayer.Count];
				int[] yr = new int[ulCoordForPlayer.Count];
				int[] zr = new int[ulCoordForPlayer.Count];
				for (int i = 0; i < xr.Length; i++)
				{
					Vec3i coord = ulCoordForPlayer[i];
					xr[i] = coord.X;
					yr[i] = coord.Y;
					zr[i] = coord.Z;
				}
				Packet_UnloadServerChunk unloadPacket = new Packet_UnloadServerChunk();
				unloadPacket.SetX(xr);
				unloadPacket.SetY(yr);
				unloadPacket.SetZ(zr);
				Packet_Server packet = new Packet_Server
				{
					Id = 11,
					UnloadChunk = unloadPacket
				};
				server.SendPacket(client.Id, packet);
			}
		}
	}

	private void SendOutOfRangeChunkUnloads(ConnectedClient client)
	{
		List<long> unloadChunkIndices = new List<long>();
		HashSet<long> keepChunkColumns = new HashSet<long>();
		int allowedChunkRadius = server.GetAllowedChunkRadius(client);
		int chunkX = ((client.Position == null) ? (server.WorldMap.MapSizeX / 2) : ((int)client.Position.X)) / MagicNum.ServerChunkSize;
		int chunkZ = ((client.Position == null) ? (server.WorldMap.MapSizeZ / 2) : ((int)client.Position.Z)) / MagicNum.ServerChunkSize;
		int chunkMapSizeX = server.WorldMap.ChunkMapSizeX;
		for (int r = 0; r <= allowedChunkRadius; r++)
		{
			ShapeUtil.LoadOctagonIndices(keepChunkColumns, chunkX, chunkZ, r, chunkMapSizeX);
		}
		foreach (long index3d2 in client.ChunkSent)
		{
			if ((int)(index3d2 / ((long)server.WorldMap.index3dMulX * (long)server.WorldMap.index3dMulZ)) < 128)
			{
				long index2d = server.WorldMap.ChunkIndex3dToIndex2d(index3d2);
				if (!keepChunkColumns.Contains(index2d))
				{
					unloadChunkIndices.Add(index3d2);
					client.RemoveMapChunkSent(index2d);
				}
			}
		}
		if (unloadChunkIndices.Count <= 0)
		{
			return;
		}
		int[] xr = new int[unloadChunkIndices.Count];
		int[] yr = new int[unloadChunkIndices.Count];
		int[] zr = new int[unloadChunkIndices.Count];
		for (int i = 0; i < xr.Length; i++)
		{
			long index3d = unloadChunkIndices[i];
			client.RemoveChunkSent(index3d);
			ServerChunk chunk = server.WorldMap.GetServerChunk(index3d);
			if (chunk != null)
			{
				int count = chunk.EntitiesCount;
				for (int j = 0; j < count; j++)
				{
					client.TrackedEntities.Remove(chunk.Entities[j].EntityId);
				}
			}
			xr[i] = (int)(index3d % server.WorldMap.index3dMulX);
			yr[i] = (int)(index3d / server.WorldMap.index3dMulX / server.WorldMap.index3dMulZ);
			zr[i] = (int)(index3d / server.WorldMap.index3dMulX % server.WorldMap.index3dMulZ);
		}
		Packet_UnloadServerChunk unloadPacket = new Packet_UnloadServerChunk();
		unloadPacket.SetX(xr);
		unloadPacket.SetY(yr);
		unloadPacket.SetZ(zr);
		Packet_Server packet = new Packet_Server
		{
			Id = 11,
			UnloadChunk = unloadPacket
		};
		server.SendPacket(client.Id, packet);
	}
}
