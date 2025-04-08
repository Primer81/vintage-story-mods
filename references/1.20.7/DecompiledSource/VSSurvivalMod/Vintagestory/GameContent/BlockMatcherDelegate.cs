using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public delegate bool BlockMatcherDelegate(BlockPos pos, Block placedblock, ItemStack withStackInHands);
