using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class NoChunkData : IChunkBlocks
{
	public int this[int index3d]
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	public int Length { get; set; }

	public static NoChunkData CreateNew(int chunksize)
	{
		return new NoChunkData
		{
			Length = chunksize * chunksize * chunksize
		};
	}

	public void ClearBlocks()
	{
	}

	public void ClearBlocksAndPrepare()
	{
	}

	public void SetBlockBulk(int index3d, int lenX, int lenZ, int value)
	{
	}

	public void SetBlockUnsafe(int index3d, int blockId)
	{
	}

	public void SetBlockAir(int index3d)
	{
	}

	public int GetBlockId(int index, int layer)
	{
		return 0;
	}

	public int GetBlockIdUnsafe(int index3d)
	{
		return 0;
	}

	public void SetFluid(int index3d, int value)
	{
		throw new NotImplementedException();
	}

	public int GetFluid(int index3d)
	{
		throw new NotImplementedException();
	}

	public void TakeBulkReadLock()
	{
	}

	public void ReleaseBulkReadLock()
	{
	}

	public bool ContainsBlock(int id)
	{
		return false;
	}

	public void FuzzyListBlockIds(List<int> reusableList)
	{
		reusableList.Add(0);
	}
}
