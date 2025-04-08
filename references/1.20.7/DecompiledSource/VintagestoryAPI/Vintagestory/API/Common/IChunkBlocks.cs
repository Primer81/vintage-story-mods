using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IChunkBlocks
{
	/// <summary>
	/// Retrieves the first solid block, if that one is empty, retrieves the first fluid block
	/// </summary>
	/// <param name="index3d"></param>
	/// <returns></returns>
	int this[int index3d] { get; set; }

	int Length { get; }

	void ClearBlocks();

	/// <summary>
	/// Same as ClearBlocks but initialises the chunkdata palette, so that SetBlockUnsafe can be used  (useful in worldgen)
	/// </summary>
	void ClearBlocksAndPrepare();

	/// <summary>
	/// Efficiently bulk-set a single block id in a chunk throughout a y-layer.  lenX will be ignored (always treated as 32), the y-position is specified in index3d, the width lenZ will be respected
	/// </summary>
	void SetBlockBulk(int index3d, int lenX, int lenZ, int value);

	/// <summary>
	/// Not threadsafe, used only in cases where we know that the chunk already has a palette (e.g. in worldgen when replacing rock with other blocks)
	/// </summary>
	/// <param name="index3d"></param>
	/// <param name="value"></param>
	void SetBlockUnsafe(int index3d, int value);

	void SetBlockAir(int index3d);

	/// <summary>
	/// Used to place blocks into the fluid layer instead of the solid blocks layer; calling code must do this
	/// </summary>
	/// <param name="index3d"></param>
	/// <param name="value"></param>
	void SetFluid(int index3d, int value);

	int GetBlockId(int index3d, int layer);

	int GetFluid(int index3d);

	/// <summary>
	/// Like get (i.e. this[]) but not threadsafe - only for use where setting and getting is guaranteed to be all on the same thread (e.g. during worldgen)
	/// </summary>
	/// <param name="index3d"></param>
	int GetBlockIdUnsafe(int index3d);

	/// <summary>
	/// Enter a locked section for bulk block reads from this ChunkData, using Unsafe read methods
	/// </summary>
	void TakeBulkReadLock();

	/// <summary>
	/// Leave a locked section for bulk block reads from this ChunkData, using Unsafe read methods
	/// </summary>
	void ReleaseBulkReadLock();

	/// <summary>
	/// Does this chunk contain any examples of the specified block?
	/// <br />(If the result is false, this is a very fast lookup because it quickly scans the blocks palette, not every block individually.)
	/// </summary>
	bool ContainsBlock(int blockId);

	/// <summary>
	/// Populates the list with all block IDs which are present in this chunk.  The list may contain false positives (i.e. blocks which used to be here but were removed) so that's why it's called a "Fuzzy" list.
	/// There will be no false negatives, therefore useful as a first-pass filter when scanning chunks for various types of block e.g. ITickable
	/// </summary>
	void FuzzyListBlockIds(List<int> reusableList);
}
