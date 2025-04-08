using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockBehaviorBreakIfFloating : BlockBehavior
{
	public BlockBehaviorBreakIfFloating(Block block)
		: base(block)
	{
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		if (world.Side != EnumAppSide.Client && (world.Api as ICoreServerAPI).Server.Config.AllowFallingBlocks)
		{
			handled = EnumHandling.PassThrough;
			if (IsSurroundedByNonSolid(world, pos))
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
			base.OnNeighbourBlockChange(world, pos, neibpos, ref handled);
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		if (IsSurroundedByNonSolid(world, pos))
		{
			handled = EnumHandling.PreventSubsequent;
			return new ItemStack[1]
			{
				new ItemStack(block)
			};
		}
		handled = EnumHandling.PassThrough;
		return null;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
	}

	public bool IsSurroundedByNonSolid(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			if (world.BlockAccessor.IsSideSolid(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z, facing.Opposite))
			{
				return false;
			}
		}
		return true;
	}
}
