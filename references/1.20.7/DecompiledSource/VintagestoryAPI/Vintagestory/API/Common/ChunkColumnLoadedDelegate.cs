using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Triggered when the server loaded a chunk column from disk or generated a new one
/// </summary>
/// <param name="chunkCoord"></param>
/// <param name="chunks"></param>
public delegate void ChunkColumnLoadedDelegate(Vec2i chunkCoord, IWorldChunk[] chunks);
