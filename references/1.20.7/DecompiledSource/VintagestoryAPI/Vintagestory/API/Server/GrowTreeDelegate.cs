using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public delegate void GrowTreeDelegate(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treeGenParams);
