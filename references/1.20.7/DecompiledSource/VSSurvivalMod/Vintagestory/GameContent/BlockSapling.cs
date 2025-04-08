using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSapling : BlockPlant
{
	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntitySapling { stage: EnumTreeGrowthStage.Seed, plantedFromSeed: not false })
		{
			return new Cuboidf[1]
			{
				new Cuboidf(0.2f, 0f, 0.2f, 0.8f, 0.1875f, 0.8f)
			};
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}
}
