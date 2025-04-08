namespace Vintagestory.GameContent;

public struct GridAndChunkIndex
{
	public int GridIndex;

	public long ChunkIndex;

	public GridAndChunkIndex(int gridIndex, long chunkIndex)
	{
		GridIndex = gridIndex;
		ChunkIndex = chunkIndex;
	}
}
