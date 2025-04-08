using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorBlockEntityInteract : BlockBehavior
{
	public BlockBehaviorBlockEntityInteract(Block block)
		: base(block)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return getInteractable(world, blockSel.Position)?.OnBlockInteractStart(world, byPlayer, blockSel, ref handling) ?? false;
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (getInteractable(world, blockSel.Position) is ILongInteractable ii)
		{
			return ii.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, ref handling);
		}
		return false;
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (getInteractable(world, blockSel.Position) is ILongInteractable ii)
		{
			return ii.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, ref handling);
		}
		return false;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		if (getInteractable(world, blockSel.Position) is ILongInteractable ii)
		{
			ii.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
		}
	}

	private IInteractable getInteractable(IWorldAccessor world, BlockPos pos)
	{
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is IInteractable ii)
		{
			return ii;
		}
		if (be == null)
		{
			return null;
		}
		foreach (BlockEntityBehavior bh in be.Behaviors)
		{
			if (bh is IInteractable)
			{
				return bh as IInteractable;
			}
		}
		return null;
	}
}
