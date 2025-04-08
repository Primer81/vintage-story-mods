using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorIgniteable : BlockBehavior
{
	public BlockBehaviorIgniteable(Block block)
		: base(block)
	{
	}

	public virtual void Ignite(IWorldAccessor world, BlockPos pos)
	{
		if (!(block.LastCodePart() == "lit"))
		{
			Block litblock = world.GetBlock(block.CodeWithParts("lit"));
			if (litblock != null)
			{
				world.BlockAccessor.ExchangeBlock(litblock.BlockId, pos);
			}
		}
	}

	public void Extinguish(IWorldAccessor world, BlockPos pos)
	{
		if (!(block.LastCodePart() == "extinct"))
		{
			Block litblock = world.GetBlock(block.CodeWithParts("extinct"));
			if (litblock != null)
			{
				world.BlockAccessor.ExchangeBlock(litblock.BlockId, pos);
			}
		}
	}
}
