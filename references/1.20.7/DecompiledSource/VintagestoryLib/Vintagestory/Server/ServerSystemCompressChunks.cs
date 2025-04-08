using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemCompressChunks : ServerSystem
{
	private long chunkCompressScanTimer;

	private object compactableChunksLock = new object();

	private Queue<long> compactableChunks = new Queue<long>();

	private object compactedChunksLock = new object();

	private Queue<long> compactedChunks = new Queue<long>();

	private object clientIdsLock = new object();

	private List<int> clientIds = new List<int>();

	public ServerSystemCompressChunks(ServerMain server)
		: base(server)
	{
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		lock (clientIdsLock)
		{
			clientIds.Add(player.ClientId);
		}
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		lock (clientIdsLock)
		{
			clientIds.Remove(player.ClientId);
		}
	}

	public override int GetUpdateInterval()
	{
		return 10;
	}

	public override void OnServerTick(float dt)
	{
		if (compactableChunks.Count <= 0)
		{
			FreeMemory();
			long cur = server.totalUnpausedTime.ElapsedMilliseconds;
			if (cur - chunkCompressScanTimer >= 4000)
			{
				chunkCompressScanTimer = cur;
				FindFreeableMemory();
			}
		}
	}

	private void FindFreeableMemory()
	{
		List<BlockPos> plrChunkPos = new List<BlockPos>();
		lock (clientIdsLock)
		{
			foreach (int clientid in clientIds)
			{
				if (server.Clients.TryGetValue(clientid, out var client) && client.State == EnumClientState.Playing)
				{
					plrChunkPos.Add(client.ChunkPos);
				}
			}
		}
		int compressed = 0;
		lock (compactableChunksLock)
		{
			server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (KeyValuePair<long, ServerChunk> val in server.loadedChunks)
				{
					if (val.Value.IsPacked())
					{
						compressed++;
					}
					else
					{
						if (Environment.TickCount - val.Value.lastReadOrWrite <= MagicNum.UncompressedChunkTTL)
						{
							continue;
						}
						bool skip = false;
						ChunkPos chunkpos = server.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
						if (!val.Value.Empty && chunkpos.Dimension == 0)
						{
							foreach (BlockPos cpos in plrChunkPos)
							{
								if (Math.Abs(cpos.X - chunkpos.X) < 2 || Math.Abs(cpos.Z - chunkpos.Z) < 2)
								{
									skip = true;
									break;
								}
							}
						}
						if (!skip)
						{
							compactableChunks.Enqueue(val.Key);
						}
					}
				}
			}
			finally
			{
				server.loadedChunksLock.ReleaseReadLock();
			}
		}
	}

	private void FreeMemory()
	{
		while (compactedChunks.Count > 0)
		{
			ServerChunk chunk = null;
			long index3d = 0L;
			lock (compactedChunksLock)
			{
				index3d = compactedChunks.Dequeue();
			}
			server.GetLoadedChunk(index3d)?.TryCommitPackAndFree(MagicNum.UncompressedChunkTTL);
		}
	}

	public override void OnSeparateThreadTick()
	{
		long index3d = 0L;
		lock (compactableChunksLock)
		{
			if (compactableChunks.Count > 0)
			{
				index3d = compactableChunks.Dequeue();
			}
		}
		if (index3d == 0L)
		{
			return;
		}
		ServerChunk chunk = server.GetLoadedChunk(index3d);
		if (chunk == null)
		{
			return;
		}
		chunk.Pack();
		lock (compactedChunksLock)
		{
			compactedChunks.Enqueue(index3d);
		}
	}
}
