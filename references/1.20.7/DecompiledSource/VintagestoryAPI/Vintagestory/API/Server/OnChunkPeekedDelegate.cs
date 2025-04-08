using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public delegate void OnChunkPeekedDelegate(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate);
