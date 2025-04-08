using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// For handling dirty chunks
/// </summary>
/// <param name="chunkCoord"></param>
/// <param name="chunk"></param>
/// <param name="reason"></param>
public delegate void ChunkDirtyDelegate(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason);
