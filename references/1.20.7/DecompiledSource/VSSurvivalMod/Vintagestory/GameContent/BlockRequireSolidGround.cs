using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRequireSolidGround : Block
{
	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (HasSolidGround(blockAccessor, pos))
		{
			return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes);
		}
		return false;
	}

	internal virtual bool HasSolidGround(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block block = blockAccessor.GetBlock(pos.Down());
		pos.Up();
		return block.SideIsSolid(blockAccessor, pos, BlockFacing.UP.Index);
	}
}
