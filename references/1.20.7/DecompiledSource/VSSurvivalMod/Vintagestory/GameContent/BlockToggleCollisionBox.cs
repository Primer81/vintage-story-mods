using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockToggleCollisionBox : BlockClutter
{
	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorToggleCollisionBox betcb = blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorToggleCollisionBox>();
		if (betcb != null && betcb.Solid)
		{
			return betcb.CollisionBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorToggleCollisionBox betcb = blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorToggleCollisionBox>();
		if (betcb != null && betcb.Solid)
		{
			return betcb.CollisionBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}
}
