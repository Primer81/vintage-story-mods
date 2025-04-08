using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ILiquidSink : ILiquidInterface
{
	void SetContent(ItemStack containerStack, ItemStack content);

	void SetContent(BlockPos pos, ItemStack content);

	int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres);

	int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres);
}
