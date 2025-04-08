using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Triggered just before a chunk column gets unloaded
/// </summary>
/// <param name="chunkCoord">chunkX and chunkZ of the column (multiply with chunksize to get position). The Y component is zero</param>
public delegate void ChunkColumnUnloadDelegate(Vec3i chunkCoord);
