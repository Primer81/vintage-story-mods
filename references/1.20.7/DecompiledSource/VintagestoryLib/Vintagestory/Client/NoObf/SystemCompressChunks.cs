using System;
using System.Collections.Generic;
using System.Runtime;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

internal class SystemCompressChunks : ClientSystem
{
	private long chunkCompressScanTimer;

	private float compressionRatio;

	private Queue<long> compactableClientChunks = new Queue<long>();

	private int lastCompactionTime;

	private float megabytesMinimum;

	private int compressed;

	private int[] ttlsByRamMode = new int[3] { 80000, 8000, 4000 };

	public override string Name => "cc";

	public SystemCompressChunks(ClientMain game)
		: base(game)
	{
		game.RegisterGameTickListener(TryCompactLargeObjectHeap, 1000);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, "cc", 0.999);
	}

	private void TryCompactLargeObjectHeap(float dt)
	{
		if (ClientSettings.OptimizeRamMode != 2)
		{
			return;
		}
		int secondsPassed = Environment.TickCount / 1000 - lastCompactionTime;
		if ((secondsPassed >= 602 || (secondsPassed >= 30 && !game.Platform.IsFocused)) && (float)(GC.GetTotalMemory(forceFullCollection: false) / 1024) / 1024f - megabytesMinimum > 512f)
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
			lastCompactionTime = Environment.TickCount / 1000;
			float mbafter = (float)(GC.GetTotalMemory(forceFullCollection: false) / 1024) / 1024f;
			if (mbafter < megabytesMinimum || megabytesMinimum == 0f)
			{
				megabytesMinimum = mbafter;
			}
		}
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 20;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		lock (game.compactSyncLock)
		{
			long index3d = 0L;
			if (compactableClientChunks.Count > 0)
			{
				index3d = compactableClientChunks.Dequeue();
			}
			if (index3d == 0L)
			{
				return;
			}
			ClientChunk chunk = null;
			lock (game.WorldMap.chunksLock)
			{
				game.WorldMap.chunks.TryGetValue(index3d, out chunk);
			}
			if (chunk != null)
			{
				chunk.Pack();
				if (!chunk.ChunkHasData())
				{
					Vec3i vec = new Vec3i();
					MapUtil.PosInt3d(index3d, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, vec);
					throw new Exception($"ACP: Chunk {vec.X} {vec.Y} {vec.Z} has no more block data.");
				}
				game.compactedClientChunks.Enqueue(index3d);
			}
		}
	}

	public void OnFinalizeFrame(float dt)
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["compactqueuesize"] = "Client Chunks in compact queue: " + compactableClientChunks.Count;
			game.DebugScreenInfo["compactratio"] = "Client chunk compression ratio: " + (compressionRatio * 100f).ToString("0.#") + "%";
		}
		else
		{
			game.DebugScreenInfo["compactqueuesize"] = "";
			game.DebugScreenInfo["compactratio"] = "";
		}
		long cur = game.Platform.EllapsedMs;
		if (cur - chunkCompressScanTimer < 4000)
		{
			return;
		}
		chunkCompressScanTimer = cur;
		int ttl = ttlsByRamMode[ClientSettings.OptimizeRamMode];
		lock (game.compactSyncLock)
		{
			lock (game.WorldMap.chunksLock)
			{
				while (game.compactedClientChunks.Count > 0)
				{
					ClientChunk chunk = null;
					long index3d = game.compactedClientChunks.Dequeue();
					game.WorldMap.chunks.TryGetValue(index3d, out chunk);
					chunk?.TryCommitPackAndFree();
				}
			}
			Vec3i chunkpos = new Vec3i();
			Vec3i plrChunkPos = new Vec3i((int)game.EntityPlayer.Pos.X, (int)game.EntityPlayer.Pos.Y, (int)game.EntityPlayer.Pos.Z) / game.WorldMap.ClientChunkSize;
			if (compactableClientChunks.Count == 0)
			{
				compressed = 0;
				lock (game.WorldMap.chunksLock)
				{
					foreach (KeyValuePair<long, ClientChunk> val in game.WorldMap.chunks)
					{
						if (val.Value.IsPacked())
						{
							compressed++;
						}
						else if (Environment.TickCount - val.Value.lastReadOrWrite > ttl && val.Value.centerModelPoolLocations != null && val.Value.edgeModelPoolLocations != null)
						{
							MapUtil.PosInt3d(val.Key, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkpos);
							if (Math.Abs(plrChunkPos.X - chunkpos.X) < 2 && Math.Abs(plrChunkPos.Z - chunkpos.Z) < 2 && !val.Value.Empty)
							{
								val.Value.MarkFresh();
							}
							else
							{
								compactableClientChunks.Enqueue(val.Key);
							}
						}
					}
				}
			}
			compressionRatio = (float)compressed / (float)game.WorldMap.chunks.Count;
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
