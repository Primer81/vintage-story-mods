using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public delegate int PlaceBlockDelegate(IBlockAccessor blockAccessor, BlockPos pos, Block newBlock, bool replaceMeta);
