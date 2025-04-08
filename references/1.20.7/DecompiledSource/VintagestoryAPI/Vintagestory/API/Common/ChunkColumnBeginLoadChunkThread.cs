namespace Vintagestory.API.Common;

/// <summary>
/// Triggered immediately when the server loads a chunk column from disk or generates a new one, in the SupplyChunks thread (not the main thread)
/// </summary>
/// <param name="mapChunk"></param>
/// <param name="chunkX"></param>
/// <param name="chunkZ"></param>
/// <param name="chunks"></param>
public delegate void ChunkColumnBeginLoadChunkThread(IServerMapChunk mapChunk, int chunkX, int chunkZ, IWorldChunk[] chunks);
