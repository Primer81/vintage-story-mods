using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ILiquidInterface
{
	bool AllowHeldLiquidTransfer { get; }

	float CapacityLitres { get; }

	float TransferSizeLitres { get; }

	float GetCurrentLitres(ItemStack containerStack);

	float GetCurrentLitres(BlockPos pos);

	bool IsFull(ItemStack containerStack);

	bool IsFull(BlockPos pos);

	WaterTightContainableProps GetContentProps(ItemStack containerStack);

	WaterTightContainableProps GetContentProps(BlockPos pos);

	ItemStack GetContent(ItemStack containerStack);

	ItemStack GetContent(BlockPos pos);
}
