using Vintagestory.API.MathTools;

namespace Vintagestory.Server;

public struct MapRegionAndPos
{
	public Vec3i pos;

	public ServerMapRegion region;

	public MapRegionAndPos(Vec3i pos, ServerMapRegion reg)
	{
		this.pos = pos;
		region = reg;
	}
}
