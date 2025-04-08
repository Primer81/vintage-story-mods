using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface ITreeGenerator
{
	void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treeGenParams, IRandom random);
}
