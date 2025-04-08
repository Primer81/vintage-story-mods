using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorPrefetch : BlockAccessorRelaxed, IBlockAccessorPrefetch, IBlockAccessor
{
	private BlockPos basePos = new BlockPos();

	private int sizeX;

	private int sizeY;

	private int sizeZ;

	private int prefetchBlocksCount;

	private List<Block> prefetchedBlocks = new List<Block>();

	private Block airBlock;

	public BlockAccessorPrefetch(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight)
		: base(worldmap, worldAccessor, synchronize, relight)
	{
		airBlock = GetBlock(new AssetLocation("air"));
	}

	public void PrefetchBlocks(BlockPos minPos, BlockPos maxPos)
	{
		prefetchBlocksCount = 0;
		basePos.Set(Math.Min(minPos.X, maxPos.X), Math.Min(minPos.Y, maxPos.Y), Math.Min(minPos.Z, maxPos.Z));
		sizeX = Math.Max(minPos.X, maxPos.X) - basePos.X + 1;
		sizeY = Math.Max(minPos.Y, maxPos.Y) - basePos.Y + 1;
		sizeZ = Math.Max(minPos.Z, maxPos.Z) - basePos.Z + 1;
		prefetchBlocksCount = sizeX * sizeY * sizeZ;
		while (prefetchedBlocks.Count < prefetchBlocksCount)
		{
			prefetchedBlocks.Add(airBlock);
		}
		WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			int num = x - basePos.X;
			int num2 = y - basePos.Y;
			int num3 = z - basePos.Z;
			prefetchedBlocks[(num2 * sizeZ + num3) * sizeX + num] = block;
		});
	}

	public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock)
	{
		int minx = GameMath.Clamp(Math.Min(minPos.X, maxPos.X), 0, worldmap.MapSizeX);
		int miny = GameMath.Clamp(Math.Min(minPos.Y, maxPos.Y), 0, worldmap.MapSizeY);
		int minz = GameMath.Clamp(Math.Min(minPos.Z, maxPos.Z), 0, worldmap.MapSizeZ);
		int maxx = GameMath.Clamp(Math.Max(minPos.X, maxPos.X), 0, worldmap.MapSizeX);
		int maxy = GameMath.Clamp(Math.Max(minPos.Y, maxPos.Y), 0, worldmap.MapSizeY);
		int maxz = GameMath.Clamp(Math.Max(minPos.Z, maxPos.Z), 0, worldmap.MapSizeZ);
		int mincx = minx / 32;
		int mincy = miny / 32;
		int mincz = minz / 32;
		int maxcx = maxx / 32;
		int maxcy = maxy / 32;
		int maxcz = maxz / 32;
		int dimensionOffsetY = minPos.dimension * 1024;
		ChunkData[] chunks = LoadChunksToCache(mincx, mincy + dimensionOffsetY, mincz, maxcx, maxcy + dimensionOffsetY, maxcz, null);
		int cxCount = maxcx - mincx + 1;
		int czCount = maxcz - mincz + 1;
		for (int y = miny; y <= maxy; y++)
		{
			int ciy = (y / 32 - mincy) * czCount - mincz;
			for (int z = minz; z <= maxz; z++)
			{
				int chunkIndexBase = (ciy + z / 32) * cxCount - mincx;
				int index3dBase = (y % 32 * 32 + z % 32) * 32;
				for (int x = minx; x <= maxx; x++)
				{
					ChunkData chunkBlocks = chunks[chunkIndexBase + x / 32];
					if (chunkBlocks != null)
					{
						int index3d = index3dBase + x % 32;
						int blockId = chunkBlocks.GetSolidBlock(index3d);
						onBlock(worldmap.Blocks[blockId], x, y, z);
					}
				}
			}
		}
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		return prefetchedBlocks[((posY - basePos.Y) * sizeZ + (posZ - basePos.Z)) * sizeX + (posX - basePos.X)].BlockId;
	}
}
