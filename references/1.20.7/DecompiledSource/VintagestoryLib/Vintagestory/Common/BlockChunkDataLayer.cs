using System;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class BlockChunkDataLayer : ChunkDataLayer
{
	public static Block[] blocksByPaletteIndex;

	public BlockChunkDataLayer(ChunkDataPool chunkDataPool)
		: base(chunkDataPool)
	{
	}

	internal void UpdateToFluidsLayer(BlockChunkDataLayer fluidsLayer)
	{
		GameMain game = pool.Game;
		for (int i = 1; i < paletteCount; i++)
		{
			Block block = game.Blocks[palette[i]];
			if (block.ForFluidsLayer)
			{
				MoveToOtherLayer(i, palette[i], fluidsLayer);
				DeleteFromPalette(i);
				i--;
			}
			else if (block.RemapToLiquidsLayer != null)
			{
				Block waterBlock = game.GetBlock(new AssetLocation(block.RemapToLiquidsLayer));
				if (waterBlock != null)
				{
					AddToOtherLayer(i, waterBlock.BlockId, fluidsLayer);
				}
			}
		}
	}

	internal void MoveToOtherLayer(int search, int fluidBlockId, BlockChunkDataLayer fluidsLayer)
	{
		int fluidPaletteIndex = fluidsLayer.GetPaletteIndex(fluidBlockId);
		readWriteLock.AcquireWriteLock();
		int bbs = bitsize;
		for (int index3d = 0; index3d < 32768; index3d += 32)
		{
			int intIndex = index3d / 32;
			int mask = -1;
			for (int j = 0; j < bbs; j++)
			{
				int v = dataBits[j][intIndex];
				mask &= ((((search >> j) & 1) == 1) ? v : (~v));
			}
			if (mask != 0)
			{
				fluidsLayer.Write(fluidPaletteIndex, intIndex, mask);
				int unsetMask = ~mask;
				for (int i = 0; i < bbs; i++)
				{
					dataBits[i][intIndex] &= unsetMask;
				}
			}
		}
		readWriteLock.ReleaseWriteLock();
	}

	internal void AddToOtherLayer(int search, int fluidBlockId, BlockChunkDataLayer fluidsLayer)
	{
		int fluidPaletteIndex = fluidsLayer.GetPaletteIndex(fluidBlockId);
		readWriteLock.AcquireReadLock();
		int bbs = bitsize;
		for (int index3d = 0; index3d < 32768; index3d += 32)
		{
			int intIndex = index3d / 32;
			int mask = -1;
			for (int i = 0; i < bbs; i++)
			{
				int v = dataBits[i][intIndex];
				mask &= ((((search >> i) & 1) == 1) ? v : (~v));
			}
			if (mask != 0)
			{
				fluidsLayer.Write(fluidPaletteIndex, intIndex, mask);
			}
		}
		readWriteLock.ReleaseReadLock();
	}

	private int GetPaletteIndex(int value)
	{
		int paletteIndex;
		if (palette != null)
		{
			paletteIndex = 0;
			while (true)
			{
				if (paletteIndex < paletteCount)
				{
					if (palette[paletteIndex] == value)
					{
						break;
					}
					paletteIndex++;
					continue;
				}
				lock (palette)
				{
					if (paletteIndex == palette.Length)
					{
						paletteIndex = MakeSpaceInPalette();
					}
					palette[paletteIndex] = value;
					paletteCount++;
				}
				break;
			}
		}
		else
		{
			if (value == 0)
			{
				return 0;
			}
			NewDataBitsWithFirstValue(value);
			paletteIndex = 1;
		}
		return paletteIndex;
	}

	private Block getBlockOne(int index3d)
	{
		int bitIndex = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[(dataBit0[index3d] >> bitIndex) & 1];
	}

	private Block getBlockTwo(int index3d)
	{
		int bitIndex = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> bitIndex) & 1) + 2 * ((dataBit1[index3d] >> bitIndex) & 1)];
	}

	private Block getBlockThree(int index3d)
	{
		int bitIndex = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> bitIndex) & 1) + 2 * ((dataBit1[index3d] >> bitIndex) & 1) + 4 * ((dataBit2[index3d] >> bitIndex) & 1)];
	}

	private Block getBlockFour(int index3d)
	{
		int bitIndex = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> bitIndex) & 1) + 2 * ((dataBit1[index3d] >> bitIndex) & 1) + 4 * ((dataBit2[index3d] >> bitIndex) & 1) + 8 * ((dataBit3[index3d] >> bitIndex) & 1)];
	}

	private Block getBlockFive(int index3d)
	{
		int bitIndex = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> bitIndex) & 1) + 2 * ((dataBit1[index3d] >> bitIndex) & 1) + 4 * ((dataBit2[index3d] >> bitIndex) & 1) + 8 * ((dataBit3[index3d] >> bitIndex) & 1) + 16 * ((dataBits[4][index3d] >> bitIndex) & 1)];
	}

	private Block getBlockGeneralCase(int index3d)
	{
		int bitIndex = index3d % 32;
		index3d /= 32;
		int bitValue = 1;
		int idx = 0;
		for (int i = 0; i < bitsize; i++)
		{
			idx += ((dataBits[i][index3d] >> bitIndex) & 1) * bitValue;
			bitValue *= 2;
		}
		return blocksByPaletteIndex[idx];
	}

	public System.Func<int, Block> SelectDelegateBlockClient(System.Func<int, Block> getBlockAir)
	{
		return bitsize switch
		{
			0 => getBlockAir, 
			1 => getBlockOne, 
			2 => getBlockTwo, 
			3 => getBlockThree, 
			4 => getBlockFour, 
			5 => getBlockFive, 
			_ => getBlockGeneralCase, 
		};
	}

	public static void Dispose()
	{
		blocksByPaletteIndex = null;
	}
}
