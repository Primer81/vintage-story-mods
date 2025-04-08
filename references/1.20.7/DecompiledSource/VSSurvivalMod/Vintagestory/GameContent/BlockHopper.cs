using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockHopper : Block, IBlockItemFlow
{
	public bool HasItemFlowConnectorAt(BlockFacing facing)
	{
		return facing == BlockFacing.DOWN;
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (facing != BlockFacing.UP || !(entity is EntityItem inWorldItem) || world.Side != EnumAppSide.Server || world.Rand.NextDouble() < 0.9)
		{
			return;
		}
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		if (!inWorldItem.Alive || !(blockEntity is BlockEntityItemFlow beItemFlow))
		{
			return;
		}
		WeightedSlot ws = beItemFlow.inventory.GetBestSuitedSlot(inWorldItem.Slot);
		if (ws.slot != null)
		{
			inWorldItem.Slot.TryPutInto(api.World, ws.slot);
			if (inWorldItem.Slot.StackSize <= 0)
			{
				inWorldItem.Itemstack = null;
				inWorldItem.Alive = false;
			}
		}
	}
}
