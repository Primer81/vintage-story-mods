using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFern : BlockPlant, ICustomTreeFellingBehavior
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		waveFlagMinY = 0.25f;
		tallGrassColorMapping = true;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);
		int[] sourcemeshFlags = sourceMesh.Flags;
		int flagsCount = sourceMesh.FlagsCount;
		int vertical = VertexFlags.PackNormal(0.0, 1.0, 0.0);
		for (int i = 0; i < flagsCount; i++)
		{
			sourcemeshFlags[i] = (sourcemeshFlags[i] & -33546241) | vertical;
		}
	}
}
