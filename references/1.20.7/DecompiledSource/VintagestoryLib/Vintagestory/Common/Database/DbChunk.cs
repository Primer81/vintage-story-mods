namespace Vintagestory.Common.Database;

public class DbChunk
{
	public ChunkPos Position;

	public byte[] Data;

	public DbChunk()
	{
	}

	public DbChunk(ChunkPos pos, byte[] data)
	{
		Position = pos;
		Data = data;
	}
}
