using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenTerraPostProcess : ModStdWorldGen
{
	private ICoreServerAPI api;

	private IWorldGenBlockAccessor blockAccessor;

	private HashSet<int> chunkVisitedNodes = new HashSet<int>();

	private List<int> solidNodes = new List<int>(40);

	private QueueOfInt bfsQueue = new QueueOfInt();

	private const int ARRAYSIZE = 41;

	private readonly int[] currentVisited = new int[68921];

	private int iteration;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.01;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		blockAccessor.BeginColumn();
		int seaLevel = TerraGenConfig.seaLevel - 1;
		int chunkY = seaLevel / 32;
		int yMax = chunks[0].MapChunk.YMax;
		int cyMax = Math.Min(yMax / 32 + 1, api.World.BlockAccessor.MapSizeY / 32);
		chunkVisitedNodes.Clear();
		for (int cy = chunkY; cy < cyMax; cy++)
		{
			IChunkBlocks chunkdata = chunks[cy].Data;
			int yStart = ((cy == 0) ? 1 : 0);
			int baseY = cy * 32;
			if (baseY < seaLevel)
			{
				yStart = seaLevel - baseY;
			}
			int yEnd = 31;
			if (baseY + yEnd > yMax)
			{
				yEnd = yMax - baseY;
			}
			for (int baseindex3d = 0; baseindex3d < 1024; baseindex3d++)
			{
				int index3d = baseindex3d + (yStart - 1) * 1024;
				int blockIdBelow = ((yStart != 0) ? chunkdata.GetBlockIdUnsafe(index3d) : chunks[cy - 1].Data.GetBlockIdUnsafe(index3d + 32768));
				for (int y = yStart; y <= yEnd; y++)
				{
					index3d += 1024;
					int blockIdUnsafe = chunkdata.GetBlockIdUnsafe(index3d);
					if (blockIdUnsafe != 0 && blockIdBelow == 0)
					{
						int x = baseindex3d % 32;
						int z = baseindex3d / 32;
						if (!chunkVisitedNodes.Contains(index3d))
						{
							deletePotentialFloatingBlocks(chunkX * 32 + x, baseY + y, chunkZ * 32 + z);
						}
					}
					blockIdBelow = blockIdUnsafe;
				}
			}
		}
	}

	private void deletePotentialFloatingBlocks(int X, int Y, int Z)
	{
		int halfSize = 20;
		solidNodes.Clear();
		bfsQueue.Clear();
		int compressedPos = (halfSize << 12) | (halfSize << 6) | halfSize;
		bfsQueue.Enqueue(compressedPos);
		solidNodes.Add(compressedPos);
		int iteration = ++this.iteration;
		int visitedIndex = (halfSize * 41 + halfSize) * 41 + halfSize;
		currentVisited[visitedIndex] = iteration;
		int baseX = X - halfSize;
		int baseY = Y - halfSize;
		int baseZ = Z - halfSize;
		BlockPos npos = new BlockPos();
		int worldHeight = api.World.BlockAccessor.MapSizeY;
		int curVisitedNodes = 1;
		while (bfsQueue.Count > 0)
		{
			compressedPos = bfsQueue.Dequeue();
			int dx = compressedPos >> 12;
			int dy = (compressedPos >> 6) & 0x3F;
			int dz = compressedPos & 0x3F;
			npos.Set(baseX + dx, baseY + dy, baseZ + dz);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			for (int i = 0; i < aLLFACES.Length; i++)
			{
				aLLFACES[i].IterateThruFacingOffsets(npos);
				if (npos.Y >= worldHeight)
				{
					continue;
				}
				dx = npos.X - baseX;
				dy = npos.Y - baseY;
				dz = npos.Z - baseZ;
				visitedIndex = (dx * 41 + dy) * 41 + dz;
				if (currentVisited[visitedIndex] == iteration)
				{
					continue;
				}
				currentVisited[visitedIndex] = iteration;
				if (blockAccessor.GetBlockId(npos.X, npos.Y, npos.Z) == 0)
				{
					continue;
				}
				int newCompressedPos = (dx << 12) | (dy << 6) | dz;
				if (++curVisitedNodes > 40)
				{
					if (!solidNodes.Contains(newCompressedPos - 64))
					{
						AddToChunkVisitedNodesIfSameChunk(npos.X, npos.Y, npos.Z, X, Y, Z);
					}
					{
						foreach (int compPos in solidNodes)
						{
							if (!solidNodes.Contains(compPos - 64))
							{
								dx = compPos >> 12;
								dy = (compPos >> 6) & 0x3F;
								dz = compPos & 0x3F;
								AddToChunkVisitedNodesIfSameChunk(baseX + dx, baseY + dy, baseZ + dz, X, Y, Z);
							}
						}
						return;
					}
				}
				solidNodes.Add(newCompressedPos);
				bfsQueue.Enqueue(newCompressedPos);
			}
		}
		foreach (int solidNode in solidNodes)
		{
			int dx = solidNode >> 12;
			int dy = (solidNode >> 6) & 0x3F;
			int dz = solidNode & 0x3F;
			npos.Set(baseX + dx, baseY + dy, baseZ + dz);
			blockAccessor.SetBlock(0, npos);
		}
	}

	private void AddToChunkVisitedNodesIfSameChunk(int nposX, int nposY, int nposZ, int origX, int origY, int origZ)
	{
		if (nposY >= origY && (nposY != origY || (nposZ >= origZ && (nposZ != origZ || nposX >= origX))) && ((nposX ^ origX) & -32) == 0 && ((nposZ ^ origZ) & -32) == 0 && ((nposY ^ origY) & -32) == 0)
		{
			int index3d = ((nposY & 0x1F) * 32 + (nposZ & 0x1F)) * 32 + (nposX & 0x1F);
			chunkVisitedNodes.Add(index3d);
		}
	}
}
