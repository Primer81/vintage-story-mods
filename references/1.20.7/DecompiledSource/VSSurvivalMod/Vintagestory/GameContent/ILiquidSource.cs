using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ILiquidSource : ILiquidInterface
{
	ItemStack TryTakeContent(ItemStack containerStack, int quantity);

	ItemStack TryTakeContent(BlockPos pos, int quantity);
}
