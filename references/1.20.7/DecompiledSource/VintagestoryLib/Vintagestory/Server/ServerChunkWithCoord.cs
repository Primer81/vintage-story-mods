using Vintagestory.Common.Database;

namespace Vintagestory.Server;

public struct ServerChunkWithCoord
{
	public ServerChunk chunk;

	public ChunkPos pos;

	public bool withEntities;
}
