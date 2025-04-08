using Vintagestory.API.MathTools;

namespace Vintagestory.Server;

public class ClientStatistics
{
	public ConnectedClient client;

	public int lastChunkX;

	public int lastChunkY;

	public int lastChunkZ;

	internal EnumClientAwarenessEvent? DetectChanges()
	{
		EnumClientAwarenessEvent? returnEvent = null;
		BlockPos chunkPos = client.ChunkPos;
		if (chunkPos.X != lastChunkX || chunkPos.InternalY != lastChunkY || chunkPos.Z != lastChunkZ)
		{
			returnEvent = EnumClientAwarenessEvent.ChunkTransition;
		}
		lastChunkX = chunkPos.X;
		lastChunkY = chunkPos.InternalY;
		lastChunkZ = chunkPos.Z;
		return returnEvent;
	}
}
