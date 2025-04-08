using System;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ChunkServerThread : ServerThread, IChunkProvider
{
	internal GameDatabase gameDatabase;

	internal ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest> requestedChunkColumns;

	internal IndexedFifoQueue<ChunkColumnLoadRequest> peekingChunkColumns;

	internal ServerSystemSupplyChunks loadsavechunks;

	internal ServerSystemLoadAndSaveGame loadsavegame;

	internal IBlockAccessor worldgenBlockAccessor;

	public bool runOffThreadSaveNow;

	public bool peekMode;

	public int additionalWorldGenThreadsCount;

	private bool additionalThreadsPaused;

	public ILogger Logger => ServerMain.Logger;

	public ChunkServerThread(ServerMain server, string threadname, CancellationToken cancellationToken)
		: base(server, threadname, cancellationToken)
	{
		int availablePasses = 5;
		additionalWorldGenThreadsCount = Math.Min(availablePasses, MagicNum.MaxWorldgenThreads - 1);
		if (additionalWorldGenThreadsCount < 0)
		{
			additionalWorldGenThreadsCount = 0;
		}
	}

	protected override void UpdatePausedStatus(bool newpause)
	{
		if (ShouldPause != additionalThreadsPaused)
		{
			TogglePause(!additionalThreadsPaused);
		}
		base.UpdatePausedStatus(newpause);
	}

	private void TogglePause(bool paused)
	{
		ServerSystemSupplyChunks supplychunks = (ServerSystemSupplyChunks)serversystems[0];
		if (paused)
		{
			supplychunks.PauseAllWorldgenThreads(1500);
			supplychunks.FullyClearGeneratingQueue();
		}
		else
		{
			supplychunks.ResumeAllWorldgenThreads();
			if (additionalWorldGenThreadsCount > 0)
			{
				ServerMain.Logger.VerboseDebug("Un-pausing all worldgen threads.");
			}
		}
		additionalThreadsPaused = paused;
	}

	public ServerChunk GetGeneratingChunkAtPos(int posX, int posY, int posZ)
	{
		return GetGeneratingChunk(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
	}

	public ServerChunk GetGeneratingChunkAtPos(BlockPos pos)
	{
		return GetGeneratingChunk(pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
	}

	public ChunkColumnLoadRequest GetChunkRequestAtPos(int posX, int posZ)
	{
		long index2d = server.WorldMap.MapChunkIndex2D(posX / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
		if (!peekMode)
		{
			return requestedChunkColumns.GetByIndex(index2d);
		}
		return peekingChunkColumns.GetByIndex(index2d);
	}

	internal ServerChunk GetGeneratingChunk(int chunkX, int chunkY, int chunkZ)
	{
		long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ChunkColumnLoadRequest chunkReq = (peekMode ? peekingChunkColumns.GetByIndex(index2d) : requestedChunkColumns.GetByIndex(index2d));
		if (chunkReq != null && chunkReq.CurrentIncompletePass > EnumWorldGenPass.None && chunkY >= 0 && chunkY < chunkReq.Chunks.Length)
		{
			return chunkReq.Chunks[chunkY];
		}
		return null;
	}

	internal ServerMapChunk GetMapChunk(long index2d)
	{
		if (!server.loadedMapChunks.TryGetValue(index2d, out var mapchunk))
		{
			return (peekMode ? peekingChunkColumns.GetByIndex(index2d) : requestedChunkColumns.GetByIndex(index2d))?.MapChunk;
		}
		return mapchunk;
	}

	internal ServerMapRegion GetMapRegion(int regionX, int regionZ)
	{
		if (!server.loadedMapRegions.TryGetValue(server.WorldMap.MapRegionIndex2D(regionX, regionZ), out var mapregion))
		{
			int blockx = regionX * server.WorldMap.RegionSize;
			int num = regionZ * server.WorldMap.RegionSize;
			int chunkx = blockx / 32;
			int chunkz = num / 32;
			long index2d = server.WorldMap.MapChunkIndex2D(chunkx, chunkz);
			return (peekMode ? peekingChunkColumns.GetByIndex(index2d) : requestedChunkColumns.GetByIndex(index2d))?.MapChunk?.MapRegion;
		}
		return mapregion;
	}

	IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetGeneratingChunk(chunkX, chunkY, chunkZ);
	}

	IWorldChunk IChunkProvider.GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed)
	{
		ServerChunk generatingChunk = GetGeneratingChunk(chunkX, chunkY, chunkZ);
		if (generatingChunk != null)
		{
			((IWorldChunk)generatingChunk).Unpack();
			return generatingChunk;
		}
		return generatingChunk;
	}

	internal bool addChunkColumnRequest(long index2d, int chunkX, int chunkZ, int clientid, EnumWorldGenPass untilPass = EnumWorldGenPass.Done, ITreeAttribute chunkLoadParams = null)
	{
		return addChunkColumnRequest(new ChunkColumnLoadRequest(index2d, chunkX, chunkZ, clientid, (int)untilPass, server)
		{
			chunkGenParams = chunkLoadParams
		});
	}

	internal bool addChunkColumnRequest(ChunkColumnLoadRequest chunkRequest)
	{
		ChunkColumnLoadRequest prevChunkReq = requestedChunkColumns.elementsByIndex.GetOrAdd(chunkRequest.mapIndex2d, chunkRequest);
		if (prevChunkReq != chunkRequest)
		{
			if (prevChunkReq.untilPass < chunkRequest.untilPass)
			{
				prevChunkReq.untilPass = chunkRequest.untilPass;
			}
			if (prevChunkReq.CurrentIncompletePass < chunkRequest.CurrentIncompletePass)
			{
				prevChunkReq.Chunks = chunkRequest.Chunks;
			}
			if (prevChunkReq.creationTime < chunkRequest.creationTime)
			{
				prevChunkReq.creationTime = chunkRequest.creationTime;
			}
			if (chunkRequest.blockingRequest && !prevChunkReq.blockingRequest)
			{
				prevChunkReq.blockingRequest = true;
			}
		}
		else
		{
			requestedChunkColumns.EnqueueWithoutAddingToIndex(chunkRequest);
		}
		return !prevChunkReq.Disposed;
	}

	internal bool EnsureMinimumWorldgenPassAt(long index2d, int chunkX, int chunkZ, int minPass, long requirorTime)
	{
		server.loadedMapChunks.TryGetValue(index2d, out var mapchunk);
		if (mapchunk != null && mapchunk.CurrentIncompletePass == EnumWorldGenPass.Done)
		{
			return true;
		}
		ChunkColumnLoadRequest prevChunkReq = requestedChunkColumns.elementsByIndex.GetOrAdd(index2d, (long index2d) => new ChunkColumnLoadRequest(index2d, chunkX, chunkZ, server.serverConsoleId, -1, server));
		if (prevChunkReq.CurrentIncompletePass_AsInt < minPass)
		{
			if (prevChunkReq.untilPass < minPass)
			{
				if (prevChunkReq.untilPass < 0)
				{
					requestedChunkColumns.EnqueueWithoutAddingToIndex(prevChunkReq);
				}
				prevChunkReq.untilPass = minPass;
				if (prevChunkReq.creationTime < requirorTime)
				{
					prevChunkReq.creationTime = requirorTime;
				}
			}
			return false;
		}
		return true;
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)server.WorldMap.index3dMulZ + chunkZ) * server.WorldMap.index3dMulX + chunkX;
	}

	public long ChunkIndex3D(EntityPos pos)
	{
		return server.WorldMap.ChunkIndex3D(pos);
	}
}
