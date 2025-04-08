using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ChunkColumnLoadRequest : ILongIndex, IChunkColumnGenerateRequest
{
	internal long mapIndex2d;

	internal HashSet<int> clientIds;

	internal ServerChunk[] Chunks;

	internal ServerMapChunk MapChunk;

	internal bool enqueuedForGenerate;

	internal int untilPass;

	internal int chunkX;

	internal int chunkZ;

	internal int dimension;

	internal ITreeAttribute chunkGenParams;

	internal int disposeOrRequeueFlags;

	internal FastRWLock generatingLock;

	internal long creationTime;

	internal bool prettified;

	internal bool blockingRequest;

	private static long counter;

	internal bool Disposed => (disposeOrRequeueFlags & 1) == 1;

	public long Index => mapIndex2d;

	public EnumWorldGenPass GenerateUntilPass => (EnumWorldGenPass)untilPass;

	public EnumWorldGenPass CurrentIncompletePass
	{
		get
		{
			if (MapChunk != null)
			{
				return MapChunk.CurrentIncompletePass;
			}
			return EnumWorldGenPass.None;
		}
		set
		{
			MapChunk.CurrentIncompletePass = value;
		}
	}

	public int CurrentIncompletePass_AsInt
	{
		get
		{
			if (MapChunk != null)
			{
				return MapChunk.currentpass;
			}
			return 0;
		}
	}

	IServerChunk[] IChunkColumnGenerateRequest.Chunks => Chunks;

	public int ChunkX => chunkX;

	public int ChunkZ => chunkZ;

	public ITreeAttribute ChunkGenParams => chunkGenParams;

	public ushort[][] NeighbourTerrainHeight { get; set; }

	public bool RequiresChunkBorderSmoothing { get; set; }

	public ChunkColumnLoadRequest(long index2d, int chunkX, int chunkZ, int clientId, int untilPass, IShutDownMonitor server)
	{
		mapIndex2d = index2d;
		clientIds = new HashSet<int>();
		clientIds.Add(clientId);
		enqueuedForGenerate = true;
		this.chunkX = chunkX;
		this.chunkZ = chunkZ;
		this.untilPass = untilPass;
		generatingLock = new FastRWLock(server);
		creationTime = counter++;
	}

	public void FlagToDispose()
	{
		Interlocked.Or(ref disposeOrRequeueFlags, 1);
	}

	public void FlagToRequeue()
	{
		Interlocked.Or(ref disposeOrRequeueFlags, 2);
	}

	internal void Unpack()
	{
		if (Chunks != null)
		{
			for (int i = 0; i < Chunks.Length; i++)
			{
				Chunks[i].Unpack();
			}
		}
	}

	internal void PackAndCommit()
	{
		if (Chunks != null)
		{
			for (int i = 0; i < Chunks.Length; i++)
			{
				Chunks[i].TryPackAndCommit();
			}
		}
	}

	internal long LastReadWrite()
	{
		if (Chunks == null)
		{
			return 0L;
		}
		return Chunks[0].lastReadOrWrite;
	}

	internal bool IsPacked()
	{
		if (Chunks == null)
		{
			return true;
		}
		return Chunks[0].IsPacked();
	}
}
