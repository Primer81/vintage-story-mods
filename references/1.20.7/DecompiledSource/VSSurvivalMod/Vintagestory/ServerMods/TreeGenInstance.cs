using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class TreeGenInstance : TreeGenParams
{
	public ITreeGenerator treeGen;

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, LCGRandom rnd)
	{
		treeGen.GrowTree(blockAccessor, pos, this, rnd);
	}
}
