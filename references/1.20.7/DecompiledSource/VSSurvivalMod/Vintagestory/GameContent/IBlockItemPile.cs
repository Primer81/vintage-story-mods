using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IBlockItemPile
{
	bool Construct(ItemSlot slot, IWorldAccessor world, BlockPos pos, IPlayer byPlayer);
}
