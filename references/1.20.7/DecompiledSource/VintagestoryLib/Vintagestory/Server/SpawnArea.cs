using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Server;

public class SpawnArea
{
	public int chunkY;

	public Vec2i[] ChunkColumnCoords;

	public Dictionary<AssetLocation, int> spawnCounts = new Dictionary<AssetLocation, int>();
}
