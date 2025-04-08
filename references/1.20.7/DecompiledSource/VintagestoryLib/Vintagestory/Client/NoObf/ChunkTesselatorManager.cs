using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ChunkTesselatorManager : ClientSystem
{
	internal int chunksize;

	private object tessChunksQueueLock = new object();

	private SortableQueue<TesselatedChunk> tessChunksQueue = new SortableQueue<TesselatedChunk>();

	private object tessChunksQueuePriorityLock = new object();

	private Queue<TesselatedChunk> tessChunksQueuePriority = new Queue<TesselatedChunk>();

	private Vec3i chunkPos = new Vec3i();

	private Vec3i tmpPos = new Vec3i();

	private int singleUploadDelayCounter;

	private bool processPrioQueue;

	public static long cumulativeTime;

	public static int cumulativeCount;

	public override string Name => "tete";

	public ChunkTesselatorManager(ClientMain game)
		: base(game)
	{
		chunksize = game.WorldMap.ClientChunkSize;
		game.eventManager.RegisterRenderer(OnBeforeFrame, EnumRenderStage.Before, "chtema", 0.99);
	}

	public override void Dispose(ClientMain game)
	{
		game.ShouldTesselateTerrain = false;
		lock (tessChunksQueueLock)
		{
			tessChunksQueue?.Clear();
			tessChunksQueue = null;
		}
		lock (tessChunksQueuePriorityLock)
		{
			tessChunksQueuePriority?.Clear();
			tessChunksQueuePriority = null;
		}
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 0;
	}

	public override void OnBlockTexturesLoaded()
	{
		game.TerrainChunkTesselator.BlockTexturesLoaded();
	}

	public void OnBeforeFrame(float dt)
	{
		RuntimeStats.chunksAwaitingTesselation = game.dirtyChunksPriority.Count + game.dirtyChunks.Count + game.dirtyChunksLast.Count;
		RuntimeStats.chunksAwaitingPooling = tessChunksQueuePriority.Count + tessChunksQueue.Count;
		int tickMaxVerticesBase = game.frustumCuller.ViewDistanceSq / 48 + 350;
		int totalVertices = 0;
		if (processPrioQueue)
		{
			lock (tessChunksQueuePriorityLock)
			{
				while (tessChunksQueuePriority.Count > 0)
				{
					TesselatedChunk tesschunk = tessChunksQueuePriority.Dequeue();
					tesschunk.chunk.queuedForUpload = false;
					ClientChunk chunk2 = game.WorldMap.GetChunkAtBlockPos(tesschunk.positionX, tesschunk.positionYAndDimension, tesschunk.positionZ);
					if (chunk2 != null)
					{
						game.chunkRenderer.AddTesselatedChunk(tesschunk, chunk2);
						singleUploadDelayCounter = 10;
						totalVertices += tesschunk.VerticesCount;
						tmpPos.Set(tesschunk.positionX / 32, tesschunk.positionYAndDimension / 32, tesschunk.positionZ / 32);
						game.eventManager?.TriggerChunkRetesselated(tmpPos, chunk2);
					}
					else
					{
						tesschunk.UnusedDispose();
					}
				}
			}
			processPrioQueue = false;
		}
		int tcqc = tessChunksQueue.Count;
		int maxVertices = tickMaxVerticesBase * (3 + tcqc / (1 << ClientSettings.ChunkVerticesUploadRateLimiter));
		if (totalVertices >= maxVertices || (tcqc < 2 && (tcqc == 0 || singleUploadDelayCounter++ < 10)))
		{
			return;
		}
		singleUploadDelayCounter = 0;
		lock (tessChunksQueueLock)
		{
			tessChunksQueue.RunForEach(delegate(TesselatedChunk eachTC)
			{
				eachTC.RecalcPriority(game.player);
			});
			tessChunksQueue.Sort();
			while (tessChunksQueue.Count > 0 && totalVertices < maxVertices)
			{
				TesselatedChunk tesschunk = tessChunksQueue.Dequeue();
				tesschunk.chunk.queuedForUpload = false;
				ClientChunk chunk = game.WorldMap.GetChunkAtBlockPos(tesschunk.positionX, tesschunk.positionYAndDimension, tesschunk.positionZ);
				if (chunk != null)
				{
					game.chunkRenderer.AddTesselatedChunk(tesschunk, chunk);
					totalVertices += tesschunk.VerticesCount;
					tmpPos.Set(tesschunk.positionX / 32, tesschunk.positionYAndDimension / 32, tesschunk.positionZ / 32);
					game.eventManager?.TriggerChunkRetesselated(tmpPos, chunk);
				}
				else
				{
					tesschunk.UnusedDispose();
				}
			}
		}
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (!game.TerrainChunkTesselator.started)
		{
			return;
		}
		MeshDataRecycler currentRecycler = MeshData.Recycler;
		if (!game.ShouldTesselateTerrain)
		{
			currentRecycler?.DoRecycling();
			return;
		}
		long index3dAndEdgeFlag = 0L;
		int count = game.dirtyChunksPriority.Count;
		while (count-- > 0)
		{
			lock (game.dirtyChunksPriorityLock)
			{
				index3dAndEdgeFlag = game.dirtyChunksPriority.Dequeue();
			}
			long index3d = index3dAndEdgeFlag;
			if (index3dAndEdgeFlag < 0)
			{
				index3d = index3dAndEdgeFlag & 0x7FFFFFFFFFFFFFFFL;
				if (game.dirtyChunksPriority.Contains(index3d))
				{
					continue;
				}
			}
			MapUtil.PosInt3d(index3d, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkPos);
			if (!game.ShouldTesselateTerrain)
			{
				break;
			}
			TesselateChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, priority: true, index3dAndEdgeFlag < 0, out var requeue2);
			if (requeue2)
			{
				lock (game.dirtyChunksPriorityLock)
				{
					game.dirtyChunksPriority.Enqueue(index3dAndEdgeFlag);
				}
			}
		}
		int TICKMAXVERTICES = (game.frustumCuller.ViewDistanceSq + 16800) * 3 / 2;
		int totalVertices = 0;
		count = game.dirtyChunks.Count;
		while (count-- > 0 && totalVertices < TICKMAXVERTICES)
		{
			lock (game.dirtyChunksLock)
			{
				if (game.dirtyChunks.Count <= 0)
				{
					break;
				}
				index3dAndEdgeFlag = game.dirtyChunks.Dequeue();
				goto IL_01e4;
			}
			IL_01e4:
			long index3d2 = index3dAndEdgeFlag;
			if (index3dAndEdgeFlag < 0)
			{
				index3d2 = index3dAndEdgeFlag & 0x7FFFFFFFFFFFFFFFL;
				if (game.dirtyChunks.Contains(index3d2))
				{
					continue;
				}
			}
			if (!game.ShouldTesselateTerrain)
			{
				break;
			}
			MapUtil.PosInt3d(index3d2, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkPos);
			totalVertices += TesselateChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, priority: false, index3dAndEdgeFlag < 0, out var requeue3);
			if (requeue3)
			{
				lock (game.dirtyChunksLock)
				{
					game.dirtyChunks.Enqueue(index3dAndEdgeFlag);
				}
			}
		}
		int i = 5;
		while (game.dirtyChunksLast.Count > 0 && i-- > 0)
		{
			lock (game.dirtyChunksLastLock)
			{
				index3dAndEdgeFlag = game.dirtyChunksLast.Dequeue();
			}
			MapUtil.PosInt3d(index3dAndEdgeFlag & 0x7FFFFFFFFFFFFFFFL, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkPos);
			if (!game.ShouldTesselateTerrain)
			{
				break;
			}
			TesselateChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, priority: false, index3dAndEdgeFlag < 0, out var requeue);
			if (requeue)
			{
				lock (game.dirtyChunksLastLock)
				{
					game.dirtyChunksLast.Enqueue(index3dAndEdgeFlag);
				}
			}
		}
		currentRecycler?.DoRecycling();
	}

	public int TesselateChunk(int chunkX, int chunkY, int chunkZ, bool priority, bool skipChunkCenter, out bool requeue)
	{
		requeue = false;
		ClientChunk chunk = game.WorldMap.GetClientChunk(chunkX, chunkY, chunkZ);
		if (chunk == null || chunk.Empty)
		{
			if (chunk != null)
			{
				chunk.quantityDrawn++;
				chunk.enquedForRedraw = false;
			}
			return 0;
		}
		ChunkTesselator terrainChunkTesselator = game.TerrainChunkTesselator;
		lock (chunk.packUnpackLock)
		{
			if (!chunk.loadedFromServer)
			{
				requeue = true;
				return 0;
			}
			if (chunk.Unpack_ReadOnly())
			{
				RuntimeStats.TCTpacked++;
			}
			else
			{
				RuntimeStats.TCTunpacked++;
			}
			chunk.queuedForUpload = true;
			chunk.lastTesselationMs = game.Platform.EllapsedMs;
			chunk.enquedForRedraw = false;
			chunk.quantityDrawn++;
			terrainChunkTesselator.vars.blockEntitiesOfChunk = chunk.BlockEntities;
			terrainChunkTesselator.vars.rainHeightMap = chunk.MapChunk?.RainHeightMap ?? CreateDummyHeightMap();
		}
		if (RuntimeStats.chunksTesselatedTotal == 0)
		{
			RuntimeStats.tesselationStart = game.Platform.EllapsedMs;
		}
		RuntimeStats.chunksTesselatedPerSecond++;
		RuntimeStats.chunksTesselatedTotal++;
		if (skipChunkCenter)
		{
			RuntimeStats.chunksTesselatedEdgeOnly++;
		}
		if (chunk.shouldSunRelight)
		{
			game.terrainIlluminator.SunRelightChunk(chunk, chunkX, chunkY, chunkZ);
		}
		int verticesCount = 0;
		TesselatedChunk tessChunk = null;
		tessChunk = new TesselatedChunk
		{
			chunk = chunk,
			CullVisible = chunk.CullVisible,
			positionX = chunkX * chunksize,
			positionYAndDimension = chunkY * chunksize,
			positionZ = chunkZ * chunksize
		};
		verticesCount = (tessChunk.VerticesCount = terrainChunkTesselator.NowProcessChunk(chunkX, chunkY, chunkZ, tessChunk, skipChunkCenter));
		if (priority)
		{
			lock (tessChunksQueuePriorityLock)
			{
				tessChunksQueuePriority?.Enqueue(tessChunk);
			}
			processPrioQueue = true;
		}
		else
		{
			lock (tessChunksQueueLock)
			{
				tessChunksQueue?.EnqueueOrMerge(tessChunk);
			}
		}
		chunk.lastTesselationMs = 0L;
		return verticesCount;
	}

	private ushort[] CreateDummyHeightMap()
	{
		ushort[] newHeightMap = new ushort[game.WorldMap.MapChunkSize * game.WorldMap.MapChunkSize];
		ushort maxHeight = (ushort)(game.WorldMap.MapSizeY - 1);
		int i;
		for (i = 0; i < newHeightMap.Length; i++)
		{
			newHeightMap[i] = maxHeight;
			newHeightMap[++i] = maxHeight;
		}
		return newHeightMap;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
